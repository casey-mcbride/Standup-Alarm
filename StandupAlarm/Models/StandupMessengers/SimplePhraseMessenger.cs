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

		// TODO(Casey): Does this need to change for each utterance added to the queue
		public const string UTTERANCE_ID = "ccc6886f-b2e6-4fa3-a3b7-cdf1746d9151";

		public const float INITIAL_PITCH = .8f;
		public const float PITCH_INCREASE = .6f;
		public const float INITIAL_SPEACH_RATE = .8f;
		public const float SPEACH_RATE_INCREASE = .2f;

		/// <summary>
		/// The pause between saying the phrase.
		/// </summary>
		private static readonly TimeSpan pauseTime = TimeSpan.FromSeconds(1);

		#endregion

		#region Fields

		private TextToSpeech speechEngine;

		private string phrase;

		private int numRepeats;

		#endregion

		#region Properties

		#endregion

		#region Initializers

		public SimplePhraseMessenger(TextToSpeech speechEngine, string phrase, int numRepeats)
		{
			this.speechEngine = speechEngine;
			this.phrase = phrase;
			this.numRepeats = numRepeats;
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
			for (int i = 0; i < numRepeats - 1; i++)
			{
				// Increase the pitch for a comical result
				speechEngine.SetPitch(INITIAL_PITCH + i * PITCH_INCREASE);
				speechEngine.SetSpeechRate(INITIAL_SPEACH_RATE + i * SPEACH_RATE_INCREASE);
				sayThePhrase();
			}

			speechEngine.SetPitch(INITIAL_PITCH);
			speechEngine.SetSpeechRate(INITIAL_SPEACH_RATE);
			sayThePhrase();
		}

		private void sayThePhrase()
		{
			speechEngine.Speak(phrase, QueueMode.Add, new Bundle(), UTTERANCE_ID);
			speechEngine.PlaySilentUtterance((int)pauseTime.TotalMilliseconds, QueueMode.Add, UTTERANCE_ID);
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