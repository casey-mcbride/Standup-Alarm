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

namespace StandupAlarm.Models
{
	/// <summary>
	/// Contains the shared state and information for the application.
	/// </summary>
	class ApplicationState
	{
		// TODOS
		// Start the alarm listener on boot
		// Way to disable the application 
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
		private static readonly TimeSpan ALARM_TIME_SINCE_DAY_START = TimeSpan.FromHours(11);

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

		public static ApplicationState GetInstance(Context applicationContext)
		{
			if (instance == null)
				instance = new ApplicationState(applicationContext.ApplicationContext);

			return instance;
		}

		private void setNextAlarm()
		{
			//DateTime nextAlarm = determineNextAlarmTime();
		}

		public void SetAlarmTimer(TimeSpan timeSpan)
		{
			Intent intent = new Intent(applicationContext, typeof(StopAlarmActivity));
			intent.SetFlags(ActivityFlags.NewTask);

			PendingIntent pendingIntent = PendingIntent.GetActivity(applicationContext, 0, intent, PendingIntentFlags.CancelCurrent);
			AlarmManager.FromContext(applicationContext).SetExactAndAllowWhileIdle(AlarmType.ElapsedRealtimeWakeup, (int)(timeSpan).TotalMilliseconds, pendingIntent);
		}

		private static DateTime determineNextAlarmTime()
		{
			DateTime alarmDate = DateTime.Now;

			DayOfWeek day = alarmDate.DayOfWeek;

			// If past the alarm time pick the next day
			if (alarmDate.TimeOfDay > ALARM_TIME_SINCE_DAY_START)
			{
				AlarmDateOffset nextAlarmDay = DAY_OF_WEEK_TO_NEXT_ALARM_DAY[alarmDate.DayOfWeek];
				alarmDate.AddDays(nextAlarmDay.DaysToNext);
			}

			// Strip the time
			alarmDate = alarmDate.Date;

			// If a forbidden wednesday, pick the next day
			if((alarmDate - WEDNESDAY_TO_SKIP).Days % 14 == 0)
			{
				AlarmDateOffset nextAlarmDay = DAY_OF_WEEK_TO_NEXT_ALARM_DAY[alarmDate.DayOfWeek];
				alarmDate.AddDays(nextAlarmDay.DaysToNext);
			}

			alarmDate = alarmDate.Add(ALARM_TIME_SINCE_DAY_START);

			return alarmDate;
		}

		#endregion
	}
}