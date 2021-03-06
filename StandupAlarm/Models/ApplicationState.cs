﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Telephony;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Util;
using StandupAlarm.Activities;
using StandupAlarm.Persistance;

namespace StandupAlarm.Models
{
	/// <summary>
	/// Contains the shared state and information for the application.
	/// </summary>
	class ApplicationState
	{
		// TODOS
		// On/off switch by hitting the side buttons?
		// Add cell tower filter

		#region Constants

		public const string DATE_TIME_TIME_OF_DAY_FORMAT_STRING = "h:mm:ss tt";

		/// <summary>
		/// How much notice the user gets before the alarm actually goes off.
		/// </summary>
		public static readonly TimeSpan SHUT_OFF_WARNING_TIME
#if DEBUG
			= TimeSpan.FromSeconds(3);
#else
			= TimeSpan.FromSeconds(15);
#endif

		/// <summary>
		/// When the alarm goes off from the start of the day.
		/// </summary>
		private static readonly TimeSpan ALARM_START_TIME_OF_DAY = TimeSpan.FromHours(11);

		/// <summary>
		/// Represents information about the next alarm date.
		/// </summary>
		private struct AlarmDateOffset
		{
			public int DaysToNext { get; private set; }
			
			public DayOfWeek Day { get; private set; }

			public AlarmDateOffset(DayOfWeek day, int daysToNext)
			{
				this.Day = day;
				this.DaysToNext = daysToNext;
			}
		}

		private static readonly Dictionary<DayOfWeek, AlarmDateOffset> DAY_OF_WEEK_TO_NEXT_ALARM_DAY = new Dictionary<DayOfWeek, AlarmDateOffset>()
		{
			{ DayOfWeek.Monday, new AlarmDateOffset(DayOfWeek.Tuesday, 1 ) },
			{ DayOfWeek.Tuesday, new AlarmDateOffset(DayOfWeek.Wednesday, 1) },
			{ DayOfWeek.Wednesday, new AlarmDateOffset(DayOfWeek.Thursday, 1) },
			{ DayOfWeek.Thursday, new AlarmDateOffset(DayOfWeek.Friday, 1) },
			{ DayOfWeek.Friday, new AlarmDateOffset(DayOfWeek.Monday, 3) },
			{ DayOfWeek.Saturday, new AlarmDateOffset(DayOfWeek.Monday, 2) },
			{ DayOfWeek.Sunday, new AlarmDateOffset(DayOfWeek.Monday, 1) },
		};

		public static string[] Permissions 
		{
			get
			{
				return new string[]
				{
					Manifest.Permission.ReceiveBootCompleted,
					Manifest.Permission.SetAlarm,
					//Manifest.Permission.DevicePower,
					//Manifest.Permission.SystemAlertWindow,
					Manifest.Permission.Vibrate,
					Manifest.Permission.WakeLock,
					Manifest.Permission.AccessCoarseLocation,
				};
			}
		}

		#endregion

		#region Fields

		private readonly Context applicationContext;

		#endregion

		#region Properties

		private static ApplicationState instance;

		#endregion

		#region Initializers

		private ApplicationState(Context applicationContext)
		{
			this.applicationContext = applicationContext;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Refreshes the state of the next alarm.
		/// </summary>
		public void SyncNextAlarm ()
		{
			if (Settings.GetIsAlarmOn(applicationContext))
				StartAlarm();
			else
				ResetAlarms();
		}

		public static ApplicationState GetInstance(Context applicationContext)
		{
			if (instance == null)
				instance = new ApplicationState(applicationContext.ApplicationContext);

			return instance;
		}

		public void StartAlarm()
		{
			SetAlarmDate(determineNextAlarmTime());
		}

		/// <summary>
		/// Cancel any pending test alarms, and if the main alarm is on restarts it.
		/// </summary>
		public void ResetAlarms()
		{
			if (Settings.GetIsAlarmOn(this.applicationContext))
			{
				// Overwrite alarms with the main one
				StartAlarm();
				Settings.AddLogMessage("Alarm Started", applicationContext);
			}
			else
			{
				//Gets or creates an intent if it exists, then cancels it
				Intent intent = createAlarmViewIntent();
				PendingIntent.GetActivity(applicationContext, 0, intent, PendingIntentFlags.UpdateCurrent).Cancel();

				StandupAlarm.Persistance.Settings.SetNextAlarmTime(null, applicationContext);

				Settings.AddLogMessage("Alarm Cancelled", applicationContext);
			}
		}

		private Intent createAlarmViewIntent()
		{
			Intent intent = new Intent(applicationContext, typeof(StopAlarmActivity));
			intent.SetFlags(ActivityFlags.NewTask);
			return intent;
		}

		public void SetAlarmTimer(TimeSpan timeSpan)
		{
			SetAlarmDate(DateTime.Now + timeSpan);
		}

		public void SetAlarmDate(DateTime alarmTime)
		{
			// If we want to have the voice alarm go off at the exact given time, we need to make sure 
			// the activity shows up earlier
			DateTime stopScreenStartTime = alarmTime.Subtract(SHUT_OFF_WARNING_TIME);

			Java.Util.Calendar calendar = Java.Util.Calendar.Instance;
			calendar.Set(CalendarField.Year, stopScreenStartTime.Year);
			calendar.Set(CalendarField.DayOfYear, stopScreenStartTime.DayOfYear);
			calendar.Set(CalendarField.HourOfDay, stopScreenStartTime.Hour);
			calendar.Set(CalendarField.Minute, stopScreenStartTime.Minute);
			calendar.Set(CalendarField.Second, stopScreenStartTime.Second);

			StandupAlarm.Persistance.Settings.SetNextAlarmTime(alarmTime, applicationContext);

			PendingIntent pendingIntent = PendingIntent.GetActivity(applicationContext, 0, createAlarmViewIntent(), PendingIntentFlags.CancelCurrent);
			AlarmManager.FromContext(applicationContext).SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, calendar.TimeInMillis , pendingIntent);

			Settings.AddLogMessage(applicationContext, "Next Alarm Info:\n\tAlarm target: {0}. \n\tStop Screen Time: {1}. \n\tCurrent time: {2}",
				 alarmTime, stopScreenStartTime, DateTime.Now);
		}

		private static DateTime determineNextAlarmTime()
		{
			DateTime alarmDate = DateTime.Now;

			DayOfWeek day = alarmDate.DayOfWeek;

			// If past the alarm time (or on a weekend) pick the next day
			if (alarmDate.TimeOfDay > ALARM_START_TIME_OF_DAY || day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
			{
				AlarmDateOffset nextAlarmDay = DAY_OF_WEEK_TO_NEXT_ALARM_DAY[alarmDate.DayOfWeek];
				alarmDate = alarmDate.AddDays(nextAlarmDay.DaysToNext);
			}

			// Strip the time
			alarmDate = alarmDate.Date;

			// If 2 weeks from the forbidden day, skip it
			if((alarmDate - Settings.GetSkippedDate(instance.applicationContext)).Days % 14 == 0)
			{
				AlarmDateOffset nextAlarmDay = DAY_OF_WEEK_TO_NEXT_ALARM_DAY[alarmDate.DayOfWeek];
				alarmDate = alarmDate.AddDays(nextAlarmDay.DaysToNext);
			}

			alarmDate = alarmDate.Add(ALARM_START_TIME_OF_DAY);

			return alarmDate;
		}

		public HashSet<int> GetNearbyCellTowerIDs()
		{
			HashSet<int> cellTowerIDsNearby = new HashSet<int>();
			TelephonyManager tm = (TelephonyManager)this.applicationContext.GetSystemService(Context.TelephonyService);
			IEnumerable<CellInfo> cellInfo = tm.AllCellInfo;

			foreach (CellInfoGsm info in cellInfo.OfType<CellInfoGsm>())
				cellTowerIDsNearby.Add(info.CellIdentity.Cid);
			foreach (CellInfoCdma info in cellInfo.OfType<CellInfoCdma>())
				cellTowerIDsNearby.Add(info.CellIdentity.BasestationId);
			foreach (CellInfoLte info in cellInfo.OfType<CellInfoLte>())
				cellTowerIDsNearby.Add(info.CellIdentity.Ci);
			foreach (CellInfoWcdma info in cellInfo.OfType<CellInfoWcdma>())
				cellTowerIDsNearby.Add(info.CellIdentity.Cid);

			// Max value means the cell tower is invalid
			cellTowerIDsNearby.Remove(int.MaxValue);

			return cellTowerIDsNearby;
		}

		#endregion
	}
}