using Android.App;
using Android.Widget;
using Android.OS;
using StandupAlarm.Models;
using System;
using StandupAlarm.Persistance;
using System.Collections.Generic;

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

		private EditText TextOneOffMessage
		{
			get { return FindViewById<EditText>(Resource.Id.textOneOffMessage); }
		}

		private EditText TextSkippedDate
		{
			get { return FindViewById<EditText>(Resource.Id.textSkippedDate); }
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

			this.TextOneOffMessage.TextChanged += (s, args) => Settings.SetOneOffMessage(TextOneOffMessage.Text, this);
			TextDebugMessage.Text = Settings.GetDebugMessage(this);

			this.TextSkippedDate.SetTextIsSelectable(true);
			this.TextSkippedDate.FocusChange += TextSkippedDate_FocusChange;

			syncSkippedDate();
			syncAlarmTimeView();
			syncOneOffMessage();
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

		private void syncOneOffMessage()
		{
			this.TextOneOffMessage.Text = Settings.GetOneOffMessage(this);
		}

		private void syncSkippedDate()
		{
			this.TextSkippedDate.Text = Settings.GetSkippedDate(this).ToString("dddd, dd MMMM yy");
		}

		protected override void OnResume()
		{
			base.OnResume();
			syncAlarmTimeView();
			syncOneOffMessage();
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


		private void TextSkippedDate_FocusChange(object sender, Android.Views.View.FocusChangeEventArgs e)
		{
			if (!e.HasFocus)
				return;

			EventHandler<DatePickerDialog.DateSetEventArgs> dateChangedCallback = (s, args) =>
			{
				HashSet<DayOfWeek> weekdays = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
				string dateText = TextSkippedDate.Text;
				DateTime newDate = args.Date;
				if (weekdays.Contains(newDate.DayOfWeek))
				{
					Settings.SetSkippedDate(newDate, this);
					Toast.MakeText(this, "Date changed", ToastLength.Short).Show();
					syncSkippedDate();

					ApplicationState.GetInstance(this).ResetAlarms();
					syncAlarmTimeView();
				}
				else
					Toast.MakeText(this, string.Format("{0} is not a valid weekday", dateText), ToastLength.Short).Show();

				TextSkippedDate.ClearFocus();
			};

			DateTime now = DateTime.Now;

			DatePickerDialog theD = new DatePickerDialog(this, dateChangedCallback, now.Year, now.Month - 1, now.Day);
			theD.CancelEvent += (s, eve) => TextSkippedDate.ClearFocus();
			theD.Show();
		}

		#endregion
	}
}

