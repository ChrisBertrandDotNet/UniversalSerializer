using System;
using System.Diagnostics;
using System.Resources;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Test_UniversalSerializer.Resources;

namespace Test_UniversalSerializer
{
    public partial class App : Application
    {
        /// <summary>
        /// Permet d'accéder facilement au frame racine de l'application téléphonique.
        /// </summary>
        /// <returns>Frame racine de l'application téléphonique.</returns>
        public static PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructeur pour l'objet Application.
        /// </summary>
        public App()
        {
            // Gestionnaire global pour les exceptions non interceptées.
            UnhandledException += Application_UnhandledException;

            // Initialisation du XAML standard
            InitializeComponent();

            // Initialisation spécifique au téléphone
            InitializePhoneApplication();

            // Initialisation de l'affichage de la langue
            InitializeLanguage();

            // Affichez des informations de profilage graphique lors du débogage.
            if (Debugger.IsAttached)
            {
                // Affichez les compteurs de fréquence des trames actuels.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Affichez les zones de l'application qui sont redessinées dans chaque frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Activez le mode de visualisation d'analyse hors production,
                // qui montre les zones d'une page sur lesquelles une accélération GPU est produite avec une superposition colorée.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Empêche l'écran de s'éteindre lorsque le débogueur est utilisé en désactivant
                // la détection de l'état inactif de l'application.
                // Attention :- À utiliser uniquement en mode de débogage. Les applications qui désactivent la détection d'inactivité de l'utilisateur continueront de s'exécuter
                // et seront alimentées par la batterie lorsque l'utilisateur ne se sert pas du téléphone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }

        // Code à exécuter lorsque l'application démarre (par exemple, à partir de Démarrer)
        // Ce code ne s'exécute pas lorsque l'application est réactivée
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
        }

        // Code à exécuter lorsque l'application est activée (affichée au premier plan)
        // Ce code ne s'exécute pas lorsque l'application est démarrée pour la première fois
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
        }

        // Code à exécuter lorsque l'application est désactivée (envoyée à l'arrière-plan)
        // Ce code ne s'exécute pas lors de la fermeture de l'application
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        // Code à exécuter lors de la fermeture de l'application (par exemple, lorsque l'utilisateur clique sur Précédent)
        // Ce code ne s'exécute pas lorsque l'application est désactivée
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code à exécuter en cas d'échec d'une navigation
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // Échec d'une navigation ; arrêt dans le débogueur
                Debugger.Break();
            }
        }

        // Code à exécuter sur les exceptions non gérées
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // Une exception non gérée s'est produite ; arrêt dans le débogueur
                Debugger.Break();
            }
        }

        #region Initialisation de l'application téléphonique

        // Éviter l'initialisation double
        private bool phoneApplicationInitialized = false;

        // Ne pas ajouter de code supplémentaire à cette méthode
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Créez le frame, mais ne le définissez pas encore comme RootVisual ; cela permet à l'écran de
            // démarrage de rester actif jusqu'à ce que l'application soit prête pour le rendu.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Gérer les erreurs de navigation
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Gérer les requêtes de réinitialisation pour effacer la pile arrière
            RootFrame.Navigated += CheckForResetNavigation;

            // Garantir de ne pas retenter l'initialisation
            phoneApplicationInitialized = true;
        }

        // Ne pas ajouter de code supplémentaire à cette méthode
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Définir le Visual racine pour permettre à l'application d'effectuer le rendu
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Supprimer ce gestionnaire, puisqu'il est devenu inutile
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        private void CheckForResetNavigation(object sender, NavigationEventArgs e)
        {
            // Si l'application a reçu une navigation de « réinitialisation », nous devons vérifier
            // sur la navigation suivante pour voir si la pile de la page doit être réinitialisée
            if (e.NavigationMode == NavigationMode.Reset)
                RootFrame.Navigated += ClearBackStackAfterReset;
        }

        private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
        {
            // Désinscrire l'événement pour qu'il ne soit plus appelé
            RootFrame.Navigated -= ClearBackStackAfterReset;

            // Effacer uniquement la pile des « nouvelles » navigations (avant) et des actualisations
            if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
                return;

            // Pour une interface utilisateur cohérente, effacez toute la pile de la page
            while (RootFrame.RemoveBackEntry() != null)
            {
                ; // ne rien faire
            }
        }

        #endregion

        // Initialise la police de l'application et le sens du flux tels qu'ils sont définis dans ses chaînes de ressource localisées.
        //
        // Pour vous assurer que la police de votre application est alignée avec les langues prises en charge et que le
        // FlowDirection pour chacune de ces langues respecte le sens habituel, ResourceLanguage
        // et ResourceFlowDirection doivent être initialisés dans chaque fichier resx pour faire correspondre ces valeurs avec la
        // culture du fichier. Par exemple :
        //
        // AppResources.es-ES.resx
        //    La valeur de ResourceLanguage doit être « es-ES »
        //    La valeur de ResourceFlowDirection doit être « LeftToRight »
        //
        // AppResources.ar-SA.resx
        //     La valeur de ResourceLanguage doit être « ar-SA »
        //     La valeur de ResourceFlowDirection doit être « RightToLeft »
        //
        // Pour plus d'informations sur la localisation des applications Windows Phone, consultez le site http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Définissez la police pour qu'elle corresponde à la langue d'affichage définie par la
                // chaîne de ressource ResourceLanguage pour chaque langue prise en charge.
                //
                // Rétablit la police de la langue neutre si la langue d'affichage
                // du téléphone n'est pas prise en charge.
                //
                // Si une erreur de compilateur est détectée, ResourceLanguage est manquant dans
                // le fichier de ressources.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Définit FlowDirection pour tous les éléments sous le frame racine en fonction de la
                // de la chaîne de ressource ResourceFlowDirection pour chaque
                // langue prise en charge.
                //
                // Si une erreur de compilateur est détectée, ResourceFlowDirection est manquant dans
                // le fichier de ressources.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // Si une exception est détectée ici, elle est probablement due au fait que
                // ResourceLanguage n'est pas correctement défini sur un code de langue pris en charge
                // ou que ResourceFlowDirection est défini sur une valeur différente de LeftToRight
                // ou RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }
    }
}