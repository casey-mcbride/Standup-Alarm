using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace StandupAlarm.Activities
{
	public class StopAlarmActivity : Activity
	{
		#region Constants

		private static readonly long[] VIBRATION_PATTERN = new long[] { 0, 500, 500, 500, 500, 500};

		#endregion

		#region Fields

		#endregion

		#region Properties

		public Button ButtonStopAlarm
		{
			get { return this.FindViewById<Button>(Resource.Id.buttonStopAlarm); }
		}

		#endregion

		#region Initializers

		protected override void OnCreate(Bundle savedInstanceState)
		{
			// Make this page show evenwhen the phone is locked
			Window.AddFlags(WindowManagerFlags.ShowWhenLocked);
			Window.AddFlags(WindowManagerFlags.KeepScreenOn);
			Window.AddFlags(WindowManagerFlags.TurnScreenOn);

			Vibrator vb = this.GetSystemService(Java.Lang.Class.FromType(typeof(Vibrator))) as Vibrator;
			if (vb != null && vb.HasVibrator)
				vb.Vibrate(VIBRATION_PATTERN, -1);

			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.StopAlarmView);

			ButtonStopAlarm.Click += ButtonStopAlarm_Click;
		}

		#endregion

		#region Methods

		protected override void OnStop()
		{
			base.OnStop();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
		}

		#endregion

		#region Event Handlers

		private void ButtonStopAlarm_Click(object sender, EventArgs e)
		{
			this.Finish();
		}

		#endregion
	}
}