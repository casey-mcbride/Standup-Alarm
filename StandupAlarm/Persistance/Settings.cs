﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
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
		private const string MAIN_PREF_FILE_KEY = "MainPreferences";

		private const string IS_ALARM_ON_KEY = "IsAlarmOn";

		private static readonly Dictionary<string, object> defaultSettingsValues = new Dictionary<string, object>()
		{
			{IS_ALARM_ON_KEY, true }
		};

		#region Helper methods

		private static string getAppSettingsKey(Context context)
		{
			// TODO(Casey): I've been told to use the application ID, not the package name, but I can't find it
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
	}
}