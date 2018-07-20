using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Speech.Tts;
using Android.Views;
using Android.Widget;
using StandupAlarm.Models;
using StandupAlarm.Models.StandupMessengers;

namespace StandupAlarm.Activities
{
	public class StopAlarmActivity : Activity, TextToSpeech.IOnInitListener
	{
		#region Constants

		private static readonly long[] VIBRATION_PATTERN = new long[] { 0, 500, 500, 500, 500, 500};

		private const string TIME_FORMAT_STRING = "s\\.fff";

		#endregion

		#region Fields

		private TextToSpeech speechEngine;

		// TODO(Casey): Add a messenger done event so this view can close itself
		private IStandupMessenger currentMessenger;

		private CountDownTimer timer;

		private bool timeDone = false;

		private bool voiceReady = false;

		#endregion

		#region Properties

		public Button ButtonStopAlarm
		{
			get { return this.FindViewById<Button>(Resource.Id.buttonStopAlarm); }
		}

		public TextView TextStartTimeDisplay
		{
			get { return this.FindViewById<TextView>(Resource.Id.textStopAlarmTimeToGoOff); }
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

			this.speechEngine = new TextToSpeech(this, this);

			TextStartTimeDisplay.Text = ApplicationState.SHUT_OFF_WARNING_TIME.TotalSeconds.ToString(TIME_FORMAT_STRING);

			timer = new StartAlarmTimer((long)ApplicationState.SHUT_OFF_WARNING_TIME.TotalMilliseconds, (long)TimeSpan.FromMilliseconds(7).TotalMilliseconds, this);
			timer.Start();
		}

		private class StartAlarmTimer : CountDownTimer
		{
			StopAlarmActivity owner;

			public StartAlarmTimer(long startMS, long countdownIntervalMS, StopAlarmActivity owner)
				:base (startMS, countdownIntervalMS)
			{
				this.owner = owner;
			}

			public override void OnTick(long millisUntilFinished)
			{
				owner.TextStartTimeDisplay.Text = TimeSpan.FromMilliseconds(millisUntilFinished).ToString(TIME_FORMAT_STRING);
			}

			public override void OnFinish()
			{
				owner.TextStartTimeDisplay.Text = "0";
				owner.timeDone = true;
				owner.speak();
			}
		}

		#endregion

		#region Methods

		protected override void OnStop()
		{
			this.speechEngine.Stop();
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
			// TODO(Casey): Merge all cleanups into one function

			timer.Cancel();
			if (currentMessenger != null)
				currentMessenger.Stop();
			this.Finish();
		}

		public void OnInit(OperationResult status)
		{
			voiceReady = true;
			speak();
		}

		private void speak()
		{
			if(voiceReady && timeDone)
			{
				// Occurs when the speech engine is initialized
				System.Diagnostics.Debug.Assert(currentMessenger == null);

				currentMessenger = MessengerFactory.CreateMessenger(speechEngine, DateTime.Now);
				currentMessenger.Start();
			}
		}

		#endregion
	}
}