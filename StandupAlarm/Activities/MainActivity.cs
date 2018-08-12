using Android.App;
using Android.Widget;
using Android.OS;
using StandupAlarm.Models;
using System;
using StandupAlarm.Persistance;

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

		private Switch SwitchIsAlarmOn
		{
			get { return FindViewById<Switch>(Resource.Id.switchIsAlarmOn); }
		}
		private Button ButtonTestAlarm
		{
			get { return FindViewById<Button>(Resource.Id.buttonTestAlarm); }
		}

		private Button ButtonStopAlarm
		{
			get { return FindViewById<Button>(Resource.Id.buttonTestStopAlarmActivity); }
		}

		private Button ButtonCancelPendingAlarms
		{
			get { return FindViewById<Button>(Resource.Id.buttonCancelPending); }
		}

		#endregion

		#region Initializers

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.Main);

			ButtonTestAlarm.Click += TestAlarmButton_Clicked;
			ButtonStopAlarm.Click += TestStopAlarmActivity_Click;
			ButtonCancelPendingAlarms.Click += ButtonCancelPendingAlarms_Click;

			SwitchIsAlarmOn.Checked = Settings.GetIsAlarmOn(this);
			SwitchIsAlarmOn.CheckedChange += SwitchIsAlarmOn_CheckedChange;
		}

		#endregion

		#region Methods

		#endregion

		#region Event Handlers

		private void SwitchIsAlarmOn_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			Settings.SetIsOn(e.IsChecked, this);

			// TODO(Casey): Move this to some global startup place
			// Update the apps timer
			if (Settings.GetIsAlarmOn(this))
				ApplicationState.GetInstance(this).StartAlarm();
			else
				ApplicationState.GetInstance(this).ResetAlarms();
		}

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

		private void ButtonCancelPendingAlarms_Click(object sender, EventArgs e)
		{
			ApplicationState.GetInstance(this).ResetAlarms();
		}

		#endregion
	}
}

