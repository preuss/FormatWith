using System;
using System.Collections.Generic;
using System.Text;

namespace FormatWith {
	/// <summary>
	/// Contains the result of the replacement including if it was a success.
	/// </summary>
	public struct ReplacementResult {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="success">The boolean value if this was a success</param>
		/// <param name="value">The object value when if was a success</param>
		public ReplacementResult(bool success, object value) {
			Success = success;
			Value = value;
		}
		/// <summary>
		/// Tells if this replacement was a success
		/// </summary>
		public bool Success { get; }
		/// <summary>
		/// The object result of the replacement.
		/// </summary>
		public object Value { get; }
	}
}
