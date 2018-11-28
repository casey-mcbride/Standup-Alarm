using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace StandupAlarm.Persistance
{
	/// <summary>
	/// Interface to settings for this app.
	/// </summary>
	public static class Settings
	{
		private const int MAX_LOG_MESSAGES = 20;

		private const string MAIN_PREF_FILE_KEY = "MainPreferences";

		private const string IS_ALARM_ON_KEY = "IsAlarmOn";

		private const string NEXT_ALARM_TIME_KEY = "NextAlarmTime";

		private const string ONE_OFF_MESSAGE_KEY = "OneOffMessage";

		private const string SKIPPED_DATE_KEY = "SkippedDate";

		private const string IS_LOGGING_ENABLED_KEY = "IsLoggingEnabled";

		private const string LOG_KEY = "Log";

		private const string VALID_CELL_TOWER_IDS_KEY = "ValidCellTowerIDs";

		private static readonly long EMPTY_DATE_TICKS = 0;

		private static readonly Dictionary<string, object> defaultSettingsValues = new Dictionary<string, object>()
		{
			{IS_ALARM_ON_KEY, false },
			{NEXT_ALARM_TIME_KEY, EMPTY_DATE_TICKS },
			{ONE_OFF_MESSAGE_KEY, string.Empty },
			{SKIPPED_DATE_KEY, DateTime.Now.Date.Ticks },
			{IS_LOGGING_ENABLED_KEY, false },
			{LOG_KEY, string.Empty },
			{VALID_CELL_TOWER_IDS_KEY, string.Empty },
		};

		#region Helper methods

		private static string getAppSettingsKey(Context context)
		{
			// NOTE: I've been told to use the application ID, not the package name, but I can't find it
			return string.Format("{0}.{1}", context.ApplicationInfo.PackageName, MAIN_PREF_FILE_KEY);
		}

		private static string getSettingsKey(string settingKey, Context context)
		{
			return string.Format("{0}.{1}", getAppSettingsKey(context), settingKey);
		}

		private static ISharedPreferences getSharedPreferences(Context context)
		{
			return context.GetSharedPreferences(getAppSettingsKey(context), FileCreationMode.Private);
		}

		private static T getSetting<T>(string settingKey, Context context)
		{
			IDictionary<string, object> settings = getSharedPreferences(context).All;
			string trueKey = getSettingsKey(settingKey, context);
			System.Diagnostics.Debug.Assert(!settings.ContainsKey(trueKey) || settings[trueKey] is T, "Invalid key type");

			if (settings.ContainsKey(trueKey) && settings[trueKey] is T)
			{
				return (T)settings[trueKey];
			}
			else
			{
				System.Diagnostics.Debug.Assert(defaultSettingsValues[settingKey] is T, "Invalid default value type");
				return (T)defaultSettingsValues[settingKey];
			}
		}

		#endregion Helper methods

		public static bool GetIsAlarmOn(Context context)
		{
			return getSetting<bool>(IS_ALARM_ON_KEY, context);
		}

		public static void SetIsOn(bool isOn, Context context)
		{
			ISharedPreferences preferences = getSharedPreferences(context);
			string settingKey = getSettingsKey(IS_ALARM_ON_KEY, context);
			using(ISharedPreferencesEditor editor = preferences.Edit())
			{
				editor.PutBoolean(settingKey, isOn);
				bool commitSuccess = editor.Commit();
				if (!commitSuccess)
					throw new ApplicationException("Unable to save the settings file");
			}
		}

		public static Nullable<DateTime> GetNextAlarmTime(Context context)
		{
			long ticks = getSetting<long>(NEXT_ALARM_TIME_KEY, context);
			if (ticks == EMPTY_DATE_TICKS)
				return null;
			else
				return new DateTime(ticks);
		}

		public static void SetNextAlarmTime(Nullable<DateTime> time, Context context)
		{
			long ticks = EMPTY_DATE_TICKS;
			if (time.HasValue)
				ticks = time.Value.Ticks;

			ISharedPreferences preferences = getSharedPreferences(context);
			string settingKey = getSettingsKey(NEXT_ALARM_TIME_KEY, context);
			using(ISharedPreferencesEditor editor = preferences.Edit())
			{
				editor.PutLong(settingKey, ticks);
				bool commitSuccess = editor.Commit();
				if (!commitSuccess)
					throw new ApplicationException("Unable to save the settings file");
			}
		}

		public static string GetOneOffMessage(Context context)
		{
			return getSetting<string>(ONE_OFF_MESSAGE_KEY, context);
		}

		public static void SetOneOffMessage(string message, Context context)
		{
			ISharedPreferences preferences = getSharedPreferences(context);
			string settingKey = getSettingsKey(ONE_OFF_MESSAGE_KEY, context);
			using (ISharedPreferencesEditor editor = preferences.Edit())
			{
				editor.PutString(settingKey, message);
				bool commitSuccess = editor.Commit();
				if (!commitSuccess)
					throw new ApplicationException("Unable to save the settings file");
			}
		}

		public static HashSet<int> GetValidCellTowerIDs(Context context)
		{
			string idList = getSetting<string>(VALID_CELL_TOWER_IDS_KEY, context);
			return new HashSet<int>(idList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(id => int.Parse(id)));
		}

		public static void SetValidCellTowerIDs(IEnumerable<int> ids, Context context)
		{
			// int.MaxValue means invalid signal
			ids = ids.Where(id => id != int.MaxValue).Distinct();

			ISharedPreferences preferences = getSharedPreferences(context);
			string settingKey = getSettingsKey(VALID_CELL_TOWER_IDS_KEY, context);
			using (ISharedPreferencesEditor editor = preferences.Edit())
			{
				string idList = string.Join(",", ids);
				editor.PutString(settingKey, idList);
				bool commitSuccess = editor.Commit();
				if (!commitSuccess)
					throw new ApplicationException("Unable to save the settings file");
			}
		}

		public static void ClearValidCellTowerIDs(Context context)
		{
			SetValidCellTowerIDs(Enumerable.Empty<int>(), context);
		}

		/// <summary>
		/// Date to skip, skip this day of the week every 2 weeks from this relative date.
		/// </summary>
		public static DateTime GetSkippedDate(Context context)
		{
			long ticks = getSetting<long>(SKIPPED_DATE_KEY, context);
			return new DateTime(ticks);
		}

		public static void SetSkippedDate(DateTime date, Context context)
		{
			// Strip off the time of day
			date = date.Date;

			ISharedPreferences preferences = getSharedPreferences(context);
			string settingKey = getSettingsKey(SKIPPED_DATE_KEY, context);
			using (ISharedPreferencesEditor editor = preferences.Edit())
			{
				editor.PutLong(settingKey, date.Ticks);
				bool commitSuccess = editor.Commit();
				if (!commitSuccess)
					throw new ApplicationException("Unable to save the settings file");
			}
		}

		public static bool GetIsLoggingEnabled(Context context)
		{
			return getSetting<bool>(IS_LOGGING_ENABLED_KEY, context);
		}

		public static void SetIsLoggingEnabled(bool isLogEnabled, Context context)
		{
			ISharedPreferences preferences = getSharedPreferences(context);
			string settingKey = getSettingsKey(IS_LOGGING_ENABLED_KEY, context);
			using (ISharedPreferencesEditor editor = preferences.Edit())
			{
				editor.PutBoolean(settingKey, isLogEnabled);
				bool commitSuccess = editor.Commit();
				if (!commitSuccess)
					throw new ApplicationException("Unable to save the settings file");
			}
		}

		public static string[] GetLog(Context context)
		{
			string logString = getSetting<string>(LOG_KEY, context);

			return logString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		}

		public static void AddLogMessage(string message, Context context)
		{
			if (!GetIsLoggingEnabled(context))
				return;

			Debug.Assert(!message.Contains(','), "We're doing a comma delimitted string :-(");
			message = message.Replace(",", "");

			// Strip the log down to the max and add the new message
			string[] logMessages = GetLog(context);
			int skipCount = Math.Max(logMessages.Length - MAX_LOG_MESSAGES + 1, 0);
			string logString = string.Format("{0},{1}", string.Join(",", logMessages.Skip(skipCount)), message);

			ISharedPreferences preferences = getSharedPreferences(context);
			string settingKey = getSettingsKey(LOG_KEY, context);
			using (ISharedPreferencesEditor editor = preferences.Edit())
			{
				editor.PutString(settingKey, logString);
				bool commitSuccess = editor.Commit();
				if (!commitSuccess)
					throw new ApplicationException("Unable to save the settings file");
			}
		}

		public static void AddLogMessage( Context context, string message, params object[] args)
		{
			if (!GetIsLoggingEnabled(context))
				return;

			AddLogMessage(string.Format(message, args), context);
		}

		public static void ClearLog(Context context)
		{
			string logString = string.Empty;

			ISharedPreferences preferences = getSharedPreferences(context);
			string settingKey = getSettingsKey(LOG_KEY, context);
			using (ISharedPreferencesEditor editor = preferences.Edit())
			{
				editor.PutString(settingKey, logString);
				bool commitSuccess = editor.Commit();
				if (!commitSuccess)
					throw new ApplicationException("Unable to save the settings file");
			}
		}
	}
}