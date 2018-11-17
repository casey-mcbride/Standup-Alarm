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
using StandupAlarm.Models;
using StandupAlarm.Persistance;

namespace StandupAlarm.BroadcastReceivers
{
	[BroadcastReceiver]
	[IntentFilter(new[] { Intent.ActionBootCompleted })]
	class BootCompletedBroadcastMessageReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
		{
			// Locked boot completed does not work :-(
			//if (intent.Action == Intent.ActionLockedBootCompleted)

			if (intent.Action == Intent.ActionBootCompleted)
			{
				ApplicationState.GetInstance(context).SyncNextAlarm();
				Settings.AddLogMessage(context, "App sync'd at {0}", DateTime.Now);
			}
		}
	}
}