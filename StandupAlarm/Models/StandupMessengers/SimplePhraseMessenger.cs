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

namespace StandupAlarm.Models.StandupMessengers
{
	/// <summary>
	/// Messenger that repeats a simple phrase a couple times.
	/// </summary>
	sealed class SimplePhraseMessenger : IStandupMessenger
	{
		#region Constants

		// TODO: Does this need to change for each utterance added to the queue
		public const string UTTERANCE_ID = "ccc6886f-b2e6-4fa3-a3b7-cdf1746d9151";

		#endregion

		#region Fields

		private TextToSpeech speechEngine;

		private string phrase;

		#endregion

		#region Properties

		#endregion

		#region Initializers

		public SimplePhraseMessenger(TextToSpeech speechEngine, string phrase)
		{
			this.speechEngine = speechEngine;
			this.phrase = phrase;
			AudioAttributes.Builder b = new AudioAttributes.Builder();
			b.SetFlags(AudioFlags.LowLatency);
			b.SetUsage(AudioUsageKind.Alarm);
			b.SetContentType(AudioContentType.Speech);

			AudioAttributes audioAttributes = b.Build();

			speechEngine.SetAudioAttributes(audioAttributes);
		}

		#endregion

		#region Methods


		public void Start()
		{
			TimeSpan delay = TimeSpan.FromSeconds(1);
			for (int i = 0; i < 5; i++)
			{
				speechEngine.Speak(phrase, QueueMode.Add, new Bundle(), UTTERANCE_ID);
				speechEngine.PlaySilentUtterance((int)delay.TotalMilliseconds, QueueMode.Add, UTTERANCE_ID);
			}
		}

		public void Stop()
		{
			speechEngine.Stop();
		}

		#endregion

		#region Event Handlers

		#endregion
	}
}