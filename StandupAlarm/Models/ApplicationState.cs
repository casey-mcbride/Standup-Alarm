using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
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

		// TODO(Casey): Switch with a flag that tells it to skip next wednesday, maybe?
		//System.Casey.Debug.Assert(Environment.UserName == "Casey" && Environment.UserDomainName == "Reamde", "Unfinished code that shouldn't have been checked in.");

		/// <summary>
		/// Wednesday to skip, skip wednesdays every 2 weeks from this relative date.
		/// </summary>
		private static readonly DateTime WEDNESDAY_TO_SKIP = DateTime.Parse("Wed 18 April 2018");

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
			setAlarmDate(determineNextAlarmTime());
		}

		/// <summary>
		/// Cancel any pending test alarms, and if the main alarm is on restarts it.
		/// </summary>
		public void ResetAlarms()
		{
			Intent intent = createAlarmViewIntent();

			if (Settings.GetIsAlarmOn(this.applicationContext))
			{
				// Overwrite alarms with the main one
				StartAlarm();
			}
			else
			{
				//Gets or creates an intent if it exists, then cancels it
				PendingIntent.GetActivity(applicationContext, 0, intent, PendingIntentFlags.UpdateCurrent).Cancel();

				StandupAlarm.Persistance.Settings.SetNextAlarmTime(null, applicationContext);
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
			setAlarmDate(DateTime.Now + timeSpan);
		}

		public void setAlarmDate(DateTime alarmTime)
		{
			int milliSecondsUntilAlarm = (int)(alarmTime - DateTime.Now).TotalMilliseconds;

			StandupAlarm.Persistance.Settings.SetNextAlarmTime(alarmTime, applicationContext);

			PendingIntent pendingIntent = PendingIntent.GetActivity(applicationContext, 0, createAlarmViewIntent(), PendingIntentFlags.CancelCurrent);
			AlarmManager.FromContext(applicationContext).SetExactAndAllowWhileIdle(AlarmType.ElapsedRealtimeWakeup, milliSecondsUntilAlarm, pendingIntent);
		}

		private static DateTime determineNextAlarmTime()
		{
			DateTime alarmDate = DateTime.Now;

			DayOfWeek day = alarmDate.DayOfWeek;

			// If past the alarm time pick the next day
			if (alarmDate.TimeOfDay > ALARM_START_TIME_OF_DAY)
			{
				AlarmDateOffset nextAlarmDay = DAY_OF_WEEK_TO_NEXT_ALARM_DAY[alarmDate.DayOfWeek];
				alarmDate = alarmDate.AddDays(nextAlarmDay.DaysToNext);
			}

			// Strip the time
			alarmDate = alarmDate.Date;

			// If a forbidden wednesday, pick the next day
			if((alarmDate - WEDNESDAY_TO_SKIP).Days % 14 == 0)
			{
				AlarmDateOffset nextAlarmDay = DAY_OF_WEEK_TO_NEXT_ALARM_DAY[alarmDate.DayOfWeek];
				alarmDate = alarmDate.AddDays(nextAlarmDay.DaysToNext);
			}

			alarmDate = alarmDate.Add(ALARM_START_TIME_OF_DAY);

			return alarmDate;
		}

		#endregion
	}
}