
// Copyright Christophe Bertrand.

using Android.App;
using Android.OS;
using Android.Widget;
using System.Linq;

namespace Tester
{
	[Activity(Label = "Tester", MainLauncher = true)]
	public class MainActivity : Activity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			Test_UniversalSerializer.Tests.RunTests(this);
			var r = Test_UniversalSerializer.Tests.TestResults;
			var failedCount = r.Count(t => !t.Success);
			var failures = r.Where(t => !t.Success).ToArray();

			var FailureCountTextView = FindViewById<TextView>(Resource.Id.FailureCount);
			FailureCountTextView.Text = failedCount.ToString();
			var testListListView = FindViewById<ListView>(Resource.Id.TestList);
			testListListView.Adapter = new TestAdapter(this, r);
			
		}
	}

}