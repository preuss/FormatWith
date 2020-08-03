using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FormatWith {
	/// <summary>
	/// This is the argument from a string replacer.
	/// The content inside braces.
	/// </summary>
	public struct ReplacementArgument {
		/// <summary>
		/// Receives the raw argument, and parses.
		/// </summary>
		/// <param name="rawArgument"></param>
		public ReplacementArgument(string rawArgument) {
			this.RawArgument = rawArgument;
			this.HasArgument = false;
			Initialize();
		}

		private void Initialize() {
			if(String.IsNullOrWhiteSpace(RawArgument)) {
				return;
			}
			HasArgument = true;
		}
		/// <summary>
		/// Returns the raw argument
		/// </summary>
		public string RawArgument { get; private set; }
		/// <summary>
		/// Tells if raw argument is empty
		/// </summary>
		public bool HasArgument { get; private set; }
		/// <summary>
		/// Returns the list of Arguments, split by ':'
		/// </summary>
		public string[] Arguments {
			get {
				return HasArgument ?
					RawArgument.Split(':').Select(s => s).ToArray()
					: 
					new string[0];
			}
		}
	}
}

