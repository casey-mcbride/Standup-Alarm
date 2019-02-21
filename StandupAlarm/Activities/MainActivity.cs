using Android.App;
using Android.Widget;
using Android.OS;
using StandupAlarm.Models;
using System;
using StandupAlarm.Persistance;
using System.Collections.Generic;
using Android;
using Android.Content;
using System.Linq;
using Android.Content.PM;
using Android.Runtime;
using System.Timers;

namespace StandupAlarm.Activities
{
	[Activity(Label = "Standup Alarm", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity
	{
		#region Constants

		static readonly TimeSpan FIND_IDS_TIMER_POLL_TIME = TimeSpan.FromSeconds(5);
		static readonly TimeSpan FIND_NEARBY_TOWERS_TOTAL_SEARCH_TIME = TimeSpan.FromSeconds(60);

		#endregion

		#region Fields

		Timer findIDsTimer;
		DateTime findIDsTimerStarted;

		#endregion

		#region Properties

		private Switch SwitchIsAlarmOn
		{
			get { return FindViewById<Switch>(Resource.Id.switchIsAlarmOn); }
		}

		private Switch SwitchRecordLog
		{
			get { return FindViewById<Switch>(Resource.Id.switchRecordLog); }
		}

		private Switch SwitchLocationConstraint
		{
			get { return FindViewById<Switch>(Resource.Id.switchLocationConstraint); }
		}

		private Button ButtonTestAlarm
		{
			get { return FindViewById<Button>(Resource.Id.buttonTestAlarm); }
		}

		private Button ButtonCustomAlarmTest
		{
			get { return FindViewById<Button>(Resource.Id.buttonCustomAlarmTest); }
		}

		private Button ButtonStopAlarm
		{
			get { return FindViewById<Button>(Resource.Id.buttonTestStopAlarmActivity); }
		}

		private Button ButtonCancelPendingAlarms
		{
			get { return FindViewById<Button>(Resource.Id.buttonCancelPending); }
		}

		private Button ButtonShowLog
		{
			get { return FindViewById<Button>(Resource.Id.buttonShowLog); }
		}

		private TextView TextNextAlarmTime
		{
			get { return FindViewById<TextView>(Resource.Id.textNextAlarmTime); }
		}

		private EditText TextOneOffMessage
		{
			get { return FindViewById<EditText>(Resource.Id.textOneOffMessage); }
		}

		private EditText TextSkippedDate
		{
			get { return FindViewById<EditText>(Resource.Id.textSkippedDate); }
		}

		private TextView TextTowersFoundsCount
		{
			get { return FindViewById<TextView>(Resource.Id.textTowersFoundCount); }
		}

		private RelativeLayout ContainerSearchForTowersProgressBar
		{
			get { return FindViewById<RelativeLayout>(Resource.Id.containerSearchForTowers); }
		}

		#endregion

		#region Initializers

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.Main);

			SwitchIsAlarmOn.Checked = Settings.GetIsAlarmOn(this);
			SwitchIsAlarmOn.CheckedChange += SwitchIsAlarmOn_CheckedChange;

			ButtonShowLog.Click += ButtonShowLog_Click;
			SwitchRecordLog.Checked = Settings.GetIsLoggingEnabled(this);
			SwitchRecordLog.CheckedChange += SwitchRecordLog_CheckedChange;

			SwitchLocationConstraint.Checked = Settings.GetValidCellTowerIDs(this).Any();
			SwitchLocationConstraint.CheckedChange += SwitchLocationConstraint_CheckedChange;

			ButtonTestAlarm.Click += TestAlarmButton_Clicked;
			ButtonCustomAlarmTest.Click += ButtonCustomAlarmTest_Click;
			ButtonStopAlarm.Click += TestStopAlarmActivity_Click;
			ButtonCancelPendingAlarms.Click += ButtonCancelPendingAlarms_Click;

			this.TextOneOffMessage.TextChanged += (s, args) => Settings.SetOneOffMessage(TextOneOffMessage.Text, this);

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

		protected override void OnStart()
		{
			base.OnStart();

			verifyPermissions();
		}

		private void verifyPermissions()
		{
			// Verify all the permissions
			if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
			{
				List<string> deniedPermissions = new List<string>();
				foreach (string permission in ApplicationState.Permissions)
				{
					// Check for permissions
					if (CheckSelfPermission(permission) != Android.Content.PM.Permission.Granted)
						deniedPermissions.Add(permission);
				}

				if(deniedPermissions.Any())
				{
					// Callback code to the application that can be handled elsewhere
					const int FAKE_REQUEST_CODE = 1;

					RequestPermissions(deniedPermissions.ToArray(), FAKE_REQUEST_CODE);
				}

			}
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
		{
			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
			List<string> deniedPermissions = new List<string>();
			for (int i = 0; i < permissions.Length; i++)
			{
				if (grantResults[i] == Permission.Denied)
					deniedPermissions.Add(permissions[i]);
			}

			if(deniedPermissions.Any())
			{
				string message = string.Format("The following permissions were denied: {0}", string.Join(", ", deniedPermissions));

				Toast.MakeText(this, message, ToastLength.Long).Show();
			}
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

		private void ButtonShowLog_Click(object sender, EventArgs e)
		{
			const char BULLET_POINT = '\u2022';
			AlertDialog.Builder dlgAlert = new AlertDialog.Builder(this);
			dlgAlert.SetMessage(string.Join("\n", Settings.GetLog(this).Reverse().Select(s =>  BULLET_POINT + s)));
			dlgAlert.SetTitle("Event Log");
			dlgAlert.SetPositiveButton("OK", null as EventHandler<DialogClickEventArgs>);
			dlgAlert.Create().Show();
		}

		private void SwitchRecordLog_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			Settings.SetIsLoggingEnabled(e.IsChecked, this);
			if(!e.IsChecked)
				Settings.ClearLog(this);
		}

		private void SwitchLocationConstraint_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
		{
			stopTowerSearch();

			ContainerSearchForTowersProgressBar.Visibility = Android.Views.ViewStates.Gone;
			if (e.IsChecked)
			{
				HashSet<int> ids = new HashSet<int>();
				findIDsTimer = new Timer
				{
					Interval = FIND_IDS_TIMER_POLL_TIME.TotalMilliseconds,
					AutoReset = true,
				};

				// Update visible count and show progress bar
				TextTowersFoundsCount.Text = string.Format("{0} Towers Found", ids.Count);
				ContainerSearchForTowersProgressBar.Visibility = Android.Views.ViewStates.Visible;

				findIDsTimer.Elapsed += (s, args) =>
				{
					this.RunOnUiThread(() =>
					{
						ids.UnionWith(ApplicationState.GetInstance(this).GetNearbyCellTowerIDs());
						Settings.SetValidCellTowerIDs(ids, this);
						TextTowersFoundsCount.Text = string.Format("{0} Towers Found", ids.Count);
						Settings.AddLogMessage(this, "Tower IDs found, thus far \"{0}\"", string.Join(", ", ids));

						if (DateTime.Now - findIDsTimerStarted > FIND_NEARBY_TOWERS_TOTAL_SEARCH_TIME)
						{
							stopTowerSearch();

							// If no ID's turn off the location constraint
							if(!ids.Any())
								SwitchLocationConstraint.Checked = false;
						}
					});
				};
				findIDsTimerStarted = DateTime.Now;
				findIDsTimer.Start();
			}
			else
			{
				Settings.ClearValidCellTowerIDs(this);
			}
		}

		private void stopTowerSearch()
		{
			if (findIDsTimer != null)
			{
				findIDsTimer.Stop();
				findIDsTimer.Dispose();
				findIDsTimer = null;
			}
			ContainerSearchForTowersProgressBar.Visibility = Android.Views.ViewStates.Gone;
		}

		private void TestAlarmButton_Clicked(object sender, System.EventArgs e)
		{
			setAlarmToGoOff(TimeSpan.FromSeconds(10) + ApplicationState.SHUT_OFF_WARNING_TIME);
		}

		private void ButtonCustomAlarmTest_Click(object sender, EventArgs e)
		{
			AlertDialog.Builder alert = new AlertDialog.Builder(this);

			alert.SetTitle("Set alarm for the future");
			alert.SetMessage("Enter minutes until alarm goes off");

			// Set an EditText view to get user input 
			EditText input = new EditText(this);
			input.InputType = Android.Text.InputTypes.ClassNumber;
			alert.SetView(input);

			alert.SetPositiveButton("Ok", (object unusedSender, DialogClickEventArgs args) =>
			{
				int minutes;
				if(int.TryParse(input.Text, out minutes) && minutes > 0)
				{
					setAlarmToGoOff(TimeSpan.FromMinutes(minutes));
				}
			});

			alert.Show();
		}

		private void setAlarmToGoOff(TimeSpan alarmTime)
		{
			Toast.MakeText(this, string.Format("Starting test in {0} seconds", alarmTime.TotalSeconds), ToastLength.Short).Show();

			ApplicationState.GetInstance(this).SetAlarmTimer(alarmTime);

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

