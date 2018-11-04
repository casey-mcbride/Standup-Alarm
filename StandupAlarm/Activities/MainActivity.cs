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

		private TextView TextNextAlarmTime
		{
			get { return FindViewById<TextView>(Resource.Id.textNextAlarmTime); }
		}

		private TextView TextDebugMessage
		{
			get { return FindViewById<TextView>(Resource.Id.textDebugMessage); }
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

			syncAlarmTimeView();

			TextDebugMessage.Text = Settings.GetDebugMessage(this);
		}

		#endregion

		#region Methods

		private void syncAlarmTimeView()
		{
			Nullable<DateTime> nextTime = Settings.GetNextAlarmTime(this);
			if (nextTime.HasValue)
				TextNextAlarmTime.Text = nextTime.Value.ToString();
			else
				TextNextAlarmTime.Text = "No alarm set";
		}

		#endregion

		#region Event Handlers

		private void SwitchIsAlarmOn_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			Settings.SetIsOn(e.IsChecked, this);

			ApplicationState.GetInstance(this).SyncNextAlarm();

			syncAlarmTimeView();
		}

		private void TestAlarmButton_Clicked(object sender, System.EventArgs e)
		{
			TimeSpan alarmTimer = TimeSpan.FromSeconds(10);
			Toast.MakeText(this, string.Format("Starting test in {0} seconds", alarmTimer.TotalSeconds), ToastLength.Short).Show();

			ApplicationState.GetInstance(this).SetAlarmTimer(alarmTimer);

			syncAlarmTimeView();
		}

		private void TestStopAlarmActivity_Click(object sender, EventArgs e)
		{
			this.StartActivity(typeof(StopAlarmActivity));
		}

		private void ButtonCancelPendingAlarms_Click(object sender, EventArgs e)
		{
			ApplicationState.GetInstance(this).ResetAlarms();

			syncAlarmTimeView();
		}

		#endregion
	}
}

