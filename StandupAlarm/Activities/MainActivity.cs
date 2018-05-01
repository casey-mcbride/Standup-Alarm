using Android.App;
using Android.Widget;
using Android.OS;
using StandupAlarm.Models;
using System;

namespace StandupAlarm.Activities
{
	[Activity(Label = "Standup Alarm", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
		#region Constants

		#endregion

		#region Fields

		#endregion

		#region Properties

		private Button TestAlarmButton
		{
			get { return FindViewById<Button>(Resource.Id.buttonTestAlarm); }
		}

		private Button TestStopAlarmActivity
		{
			get { return FindViewById<Button>(Resource.Id.buttonTestStopAlarmActivity); }
		}

		#endregion

		#region Initializers

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.Main);

			TestAlarmButton.Click += TestAlarmButton_Clicked;
			TestStopAlarmActivity.Click += TestStopAlarmActivity_Click;
		}

		#endregion

		#region Methods

		#endregion

		#region Event Handlers

		private void TestAlarmButton_Clicked(object sender, System.EventArgs e)
		{
			TimeSpan alarmTimer = TimeSpan.FromSeconds(10);
			Toast.MakeText(this, string.Format("Starting test in {0} seconds", alarmTimer.TotalSeconds), ToastLength.Short).Show();

			ApplicationState.GetInstance(this).SetAlarmTimer(alarmTimer);
		}

		private void TestStopAlarmActivity_Click(object sender, EventArgs e)
		{
			this.StartActivity(typeof(StopAlarmActivity));
		}

		#endregion
	}
}

