
// Copyright Christophe Bertrand.

using Android.App;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Test_UniversalSerializer;

namespace Tester
{
	public class TestAdapter : BaseAdapter<Test_UniversalSerializer.Tests.TestResult>
	{
		readonly IList<Test_UniversalSerializer.Tests.TestResult> items;
		readonly Activity context;

		public TestAdapter(Activity context, IList<Test_UniversalSerializer.Tests.TestResult> tests)
		{
			this.context = context;
			this.items = tests;
		}

		public override Tests.TestResult this[int position] => items[position];

		public override int Count => this.items.Count;

		public override long GetItemId(int position)
		{
			return position;
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			var item = items[position];

			View view = convertView;
			if (view == null) // no view to re-use, create new
				view = context.LayoutInflater.Inflate(Resource.Layout.ListItem, null);
			view.FindViewById<TextView>(Resource.Id.Column0).Text = item.Order.ToString();
			view.FindViewById<TextView>(Resource.Id.Column1).Text = item.Title;
			view.FindViewById<CheckBox>(Resource.Id.Column2).Checked = item.Success;
			view.FindViewById<TextView>(Resource.Id.Column3).Text = item.Error;

			return view;
		}
	}
}