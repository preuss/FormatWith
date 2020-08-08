using FormatWith.Internal;
using FormatWith.Replacer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FormatWith.Internal {
	internal static class FormatWithMethods {
		public static string FormatWith(
			string formatString,
			IDictionary<string, string> replacements,
			MissingKeyBehaviour missingKeyBehaviour = MissingKeyBehaviour.ThrowException,
			string fallbackReplacementValue = null,
			char openBraceChar = '{',
			char closeBraceChar = '}'
		) {
			return FormatWith(
				formatString,
				(key, format) => new ReplacementResult(replacements.TryGetValue(key, out string value), value),
				missingKeyBehaviour,
				fallbackReplacementValue,
				openBraceChar,
				closeBraceChar
			);
		}

		public static string FormatWith(
			string formatString,
			IDictionary<string, object> replacements,
			MissingKeyBehaviour missingKeyBehaviour = MissingKeyBehaviour.ThrowException,
			object fallbackReplacementValue = null,
			char openBraceChar = '{',
			char closeBraceChar = '}'
		) {
			return FormatWith(
				formatString,
				(key, format) => new ReplacementResult(replacements.TryGetValue(key, out object value), value),
				missingKeyBehaviour,
				fallbackReplacementValue,
				openBraceChar,
				closeBraceChar
			);
		}

		private static BindingFlags propertyBindingFlags = BindingFlags.Instance | BindingFlags.Public;

		public static string FormatWith(
			string formatString,
			object replacementObject,
			MissingKeyBehaviour missingKeyBehaviour = MissingKeyBehaviour.ThrowException,
			object fallbackReplacementValue = null,
			char openBraceChar = '{',
			char closeBraceChar = '}'
		) {
			if(replacementObject == null) throw new ArgumentNullException(nameof(replacementObject));

			return FormatWith(formatString,
				(key, format) => FromReplacementObject(key, replacementObject),
				missingKeyBehaviour,
				fallbackReplacementValue,
				openBraceChar,
				closeBraceChar
			);
		}

		public static string FormatWith(
			string formatString,
			Func<string, ReplacementArgument, ReplacementResult> handler,
			MissingKeyBehaviour missingKeyBehaviour = MissingKeyBehaviour.ThrowException,
			object fallbackReplacementValue = null,
			char openBraceChar = '{',
			char closeBraceChar = '}'
		) {
			if(formatString.Length == 0) return string.Empty;

			// get the parameters from the format string
			IEnumerable<FormatToken> tokens = FormatHelpers.Tokenize(formatString, openBraceChar, closeBraceChar);
			return FormatHelpers.ProcessTokens(tokens, handler, missingKeyBehaviour, fallbackReplacementValue, formatString.Length * 2);
		}

		public static FormattableString FormattableWith(
			string formatString,
			IDictionary<string, string> replacements,
			MissingKeyBehaviour missingKeyBehaviour = MissingKeyBehaviour.ThrowException,
			string fallbackReplacementValue = null,
			char openBraceChar = '{',
			char closeBraceChar = '}'
		) {
			return FormattableWith(
				formatString,
				key => new ReplacementResult(replacements.TryGetValue(key, out string value), value),
				missingKeyBehaviour,
				fallbackReplacementValue,
				openBraceChar,
				closeBraceChar
			);
		}

		public static FormattableString FormattableWith(
			string formatString,
			IDictionary<string, object> replacements,
			MissingKeyBehaviour missingKeyBehaviour = MissingKeyBehaviour.ThrowException,
			object fallbackReplacementValue = null,
			char openBraceChar = '{',
			char closeBraceChar = '}'
		) {
			return FormattableWith(
				formatString,
				key => new ReplacementResult(replacements.TryGetValue(key, out object value), value),
				missingKeyBehaviour,
				fallbackReplacementValue,
				openBraceChar,
				closeBraceChar
			);
		}

		public static FormattableString FormattableWith(
			string formatString,
			object replacementObject,
			MissingKeyBehaviour missingKeyBehaviour = MissingKeyBehaviour.ThrowException,
			object fallbackReplacementValue = null,
			char openBraceChar = '{',
			char closeBraceChar = '}'
		) {
			return FormattableWith(formatString,
				key => FromReplacementObject(key, replacementObject),
				missingKeyBehaviour,
				fallbackReplacementValue,
				openBraceChar,
				closeBraceChar
			);
		}

		public static FormattableString FormattableWith(
			string formatString,
			Func<string, ReplacementResult> handler,
			MissingKeyBehaviour missingKeyBehaviour = MissingKeyBehaviour.ThrowException,
			object fallbackReplacementValue = null,
			char openBraceChar = '{',
			char closeBraceChar = '}'
		) {
			// get the parameters from the format string
			IEnumerable<FormatToken> tokens = FormatHelpers.Tokenize(formatString, openBraceChar, closeBraceChar);
			return FormatHelpers.ProcessTokensIntoFormattableString(tokens, handler, missingKeyBehaviour, fallbackReplacementValue, formatString.Length * 2);
		}

		/// <summary>
		/// Gets an <see cref="IEnumerable{String}"/> that will return all format parameters used within the format string.
		/// </summary>
		/// <param name="formatString">The format string to be parsed</param>
		/// <param name="openBraceChar">The character used to begin parameters</param>
		/// <param name="closeBraceChar">The character used to end parameters</param>
		/// <returns></returns>
		public static IEnumerable<string> GetFormatParameters(
			string formatString,
			char openBraceChar = '{',
			char closeBraceChar = '}'
		) {
			return FormatHelpers.Tokenize(formatString, openBraceChar, closeBraceChar)
				.Where(t => t.TokenType == TokenType.Parameter)
				.Select(pt => pt.Value);
		}

		private static ReplacementResult FromReplacementObject(string key, object replacementObject) {
			return RetrieveByPropertyAndGetMethodWithDefault(key, replacementObject);
		}
		private static ReplacementResult RetrieveByPropertyAndGetMethodWithDefault(string propertyNotation, object replacementObject, String defaultValue = null, BindingFlags bindingsFlags = BindingFlags.Public | BindingFlags.Instance) {
			ReplacementResult failureResult = new ReplacementResult(false, defaultValue);

			// need to split this into accessors so we can traverse nested objects
			string[] propertyNotationElements = propertyNotation.Split(new[] { "." }, StringSplitOptions.None);
			
			object currentObject = replacementObject;
			foreach(string propertyNotationElement in propertyNotationElements) {
				if(currentObject == null) return failureResult;

				bool foundNext = false;
				object nextObject = null;

				PropertyInfo propertyInfo = null;
				MethodInfo methodInfo = null;
				FieldInfo fieldInfo = null;
				
				propertyInfo = currentObject.GetType().GetProperty(propertyNotationElement, bindingsFlags);
				if(propertyInfo != null) {
					nextObject = propertyInfo.GetValue(currentObject);
					foundNext = true;
				} 
				
				if(!foundNext && !String.IsNullOrEmpty(propertyNotationElement)) {
					// Try using Get method.
					string getMethod = "";
					
					// Only if not uppercase then prepend Get in front.
					if(!HasFirstLetterUpperCase(propertyNotationElement)) {
						getMethod = "Get" + ToUpperCaseFirstLetter(propertyNotationElement);
					} else {
						getMethod = propertyNotationElement;
					}
					methodInfo = currentObject.GetType().GetMethod(getMethod, 
						bindingsFlags,
						null,
						CallingConventions.Any,
						Type.EmptyTypes,
						null
						);
					if(methodInfo != null && methodInfo.ReturnType != typeof(void)) {
						nextObject = methodInfo.Invoke(currentObject, null);
						foundNext = true;
					} 
				} 
				
				if(!foundNext) {
					// Testing field value
					fieldInfo = currentObject.GetType().GetField(propertyNotationElement, bindingsFlags);
					if(fieldInfo != null) {
						nextObject = fieldInfo.GetValue(currentObject);
						foundNext = true;
					}
				}

				if(!foundNext) {
					return failureResult;
				} else {
					currentObject = nextObject;
				}
			}
			return new ReplacementResult(true, currentObject);
		}

		/// <summary>
		/// Tells if the first letter in word is upper case.
		/// </summary>
		/// <param name="word">The word to test if it has upper case first letter, but can not be null</param>
		/// <returns>Tells if the first letter of word is upper case</returns>
		/// <exception cref="ArgumentOutOfRangeException">The word needs to be at least size of one</exception>
		private static bool HasFirstLetterUpperCase(string word) {
			if(String.IsNullOrEmpty(word)) {
				throw new ArgumentOutOfRangeException("word", word, "The word can not be testet because it's null or empty string");
			}
			return Char.IsUpper(word, 0);
		}
		private static string ToUpperCaseFirstLetter(string word) {
			var wordAsChars = word.ToCharArray();
			var stringBuilder = new StringBuilder(word);
			wordAsChars[0] = Char.ToUpper(wordAsChars[0]);
			return new string(wordAsChars);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="formatString">The format string to be parsed </param>
		/// <param name="keyValues"></param>
		/// <returns></returns>
		private static string FormatWithFunc(this string formatString, IDictionary<string, object> keyValues) {
			Func<string, ReplacementArgument, ReplacementResult> formatWithFunc = (parameter, argument) => {

				if(keyValues.ContainsKey(parameter)) {
					var parameterValue = keyValues[parameter];
					if(typeof(IReplacer).IsAssignableFrom(parameterValue.GetType())) {
						return new ReplacementResult(true, ((IReplacer)parameterValue).Format(parameter, argument));
					} else if(typeof(IFormattable).IsAssignableFrom(parameterValue.GetType())) {
						return new ReplacementResult(true, ((IFormattable)parameterValue).ToString(argument.RawArgument, null));
					} else {
						string replacer = string.Join(":", argument.Arguments).Trim();
						if(argument.HasArgument) {
							replacer = "{0:" + replacer + "}";
						} else {
							replacer = "{0}";
						}
						return new ReplacementResult(true, string.Format(replacer, keyValues[parameter]));
					}
				} else {
					return new ReplacementResult(false, parameter);
				}
			};

			return formatString.FormatWith(formatWithFunc);
		}
	}
}
