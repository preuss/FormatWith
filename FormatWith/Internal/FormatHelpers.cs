﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace FormatWith.Internal {
	/// <summary>
	/// Contains all string processing and tokenizing methods for FormatWith
	/// </summary>
	internal static class FormatHelpers {
		/// <summary>
		/// Processes a list of format tokens into a string
		/// </summary>
		/// <param name="tokens">List of tokens to turn into a string</param>
		/// <param name="replacementHandler">An <see cref="Func{T, TResult}"/> using {token} as input and injects values into the string and formats the result</param>
		/// <param name="missingKeyBehaviour">The behaviour to use when the format string contains a parameter that is not present in the lookup dictionary</param>
		/// <param name="fallbackReplacementValue">When the <see cref="MissingKeyBehaviour.ReplaceWithFallback"/> is specified, this string is used as a fallback replacement value when the parameter is present in the lookup dictionary.</param>
		/// <param name="outputLengthHint">This is only an optimization hint for the internal of the <see cref="StringBuilder"/> initial size.</param>
		/// <returns>The processed result of joining the tokens with the replacement dictionary.</returns>
		public static string ProcessTokens(
			IEnumerable<FormatToken> tokens,
			Func<string, ReplacementArgument, ReplacementResult> replacementHandler,
			MissingKeyBehaviour missingKeyBehaviour,
			object fallbackReplacementValue,
			int outputLengthHint
		) {
			// create a StringBuilder to hold the resultant output string
			// use the input hint as the initial size
			StringBuilder resultBuilder = new StringBuilder(outputLengthHint);

			foreach(FormatToken thisToken in tokens) {
				if(thisToken.TokenType == TokenType.Text) {
					// token is a text token
					// add the token to the result string builder
					resultBuilder.Append(thisToken.SourceString, thisToken.StartIndex, thisToken.Length);
				} else if(thisToken.TokenType == TokenType.Parameter) {
					// token is a parameter token
					// perform parameter logic now.
					var tokenKey = thisToken.Value;
					string format = null;
					var separatorIdx = tokenKey.IndexOf(":", StringComparison.Ordinal);
					if(separatorIdx > -1) {
						tokenKey = thisToken.Value.Substring(0, separatorIdx);
						format = thisToken.Value.Substring(separatorIdx + 1);
					}

					// append the replacement for this parameter
					ReplacementResult replacementResult = replacementHandler(tokenKey, new ReplacementArgument(format));

					if(replacementResult.Success) {
						// the key exists, add the replacement value
						// this does nothing if replacement value is null
						if(string.IsNullOrWhiteSpace(format)) {
							resultBuilder.Append(replacementResult.Value);
						} else {
							resultBuilder.AppendFormat("{0:" + format + "}", replacementResult.Value);
						}
					} else {
						// the key does not exist, handle this using the missing key behaviour specified.
						switch(missingKeyBehaviour) {
							case MissingKeyBehaviour.ThrowException:
								// the key was not found as a possible replacement, throw exception
								throw new KeyNotFoundException($"The parameter \"{thisToken.Value}\" was not present in the lookup dictionary");
							case MissingKeyBehaviour.ReplaceWithFallback:
								resultBuilder.Append(fallbackReplacementValue);
								break;
							case MissingKeyBehaviour.Ignore:
								// the replacement value is the input key as a parameter.
								// use source string and start/length directly with append rather than
								// parameter.ParameterKey to avoid allocating an extra string
								resultBuilder.Append(thisToken.SourceString, thisToken.StartIndex, thisToken.Length);
								break;
						}
					}
				}
			}

			// return the resultant string
			return resultBuilder.ToString();
		}

		/// <summary>
		/// Processes a list of format tokens into a string
		/// </summary>
		/// <param name="tokens">List of tokens to turn into a string</param>
		/// <param name="replacementHandler">An <see cref="Func{T, TResult}"/> using {token} as input and injects values into the string and formats the result</param>
		/// <param name="missingKeyBehaviour">The behaviour to use when the format string contains a parameter that is not present in the lookup dictionary</param>
		/// <param name="fallbackReplacementValue">When the <see cref="MissingKeyBehaviour.ReplaceWithFallback"/> is specified, this string is used as a fallback replacement value when the parameter is present in the lookup dictionary.</param>
		/// <param name="outputLengthHint">This is only an optimization hint for the internal of the <see cref="StringBuilder"/> initial size.</param>
		/// <returns>The processed result of joining the tokens with the replacement dictionary.</returns>
		public static FormattableString ProcessTokensIntoFormattableString(
			IEnumerable<FormatToken> tokens,
			Func<string, ReplacementResult> replacementHandler,
			MissingKeyBehaviour missingKeyBehaviour,
			object fallbackReplacementValue,
			int outputLengthHint
		) {
			List<object> replacementParams = new List<object>();

			// create a StringBuilder to hold the resultant output string
			// use the input hint as the initial size
			StringBuilder resultBuilder = new StringBuilder(outputLengthHint);

			// this is the index of the current placeholder in the composite format string
			int placeholderIndex = 0;

			foreach(FormatToken thisToken in tokens) {
				if(thisToken.TokenType == TokenType.Text) {
					// token is a text token.
					// add the token to the result string builder.
					// because this text is going into a standard composite format string,
					// any instaces of { or } must be escaped with {{ and }}
					resultBuilder.AppendWithEscapedBrackets(thisToken.SourceString, thisToken.StartIndex, thisToken.Length);
				} else if(thisToken.TokenType == TokenType.Parameter) {
					// token is a parameter token
					// perform parameter logic now.
					var tokenKey = thisToken.Value;
					string format = null;
					var separatorIdx = tokenKey.IndexOf(":", StringComparison.Ordinal);
					if(separatorIdx > -1) {
						tokenKey = thisToken.Value.Substring(0, separatorIdx);
						format = thisToken.Value.Substring(separatorIdx + 1);
					}

					// append the replacement for this parameter
					ReplacementResult replacementResult = replacementHandler(tokenKey);

					string IndexAndFormat() {
						if(string.IsNullOrWhiteSpace(format)) {
							return "{" + placeholderIndex + "}";
						}

						return "{" + placeholderIndex + ":" + format + "}";
					}

					// append the replacement for this parameter
					if(replacementResult.Success) {
						// Instead of appending the replacement value directly as before,
						// append the next placeholder with the current placeholder index.
						// Add the actual replacement format item into the replacement values.
						resultBuilder.Append(IndexAndFormat());
						placeholderIndex++;
						replacementParams.Add(replacementResult.Value);
					} else {
						// the key does not exist, handle this using the missing key behaviour specified.
						switch(missingKeyBehaviour) {
							case MissingKeyBehaviour.ThrowException:
								// the key was not found as a possible replacement, throw exception
								throw new KeyNotFoundException($"The parameter \"{thisToken.Value}\" was not present in the lookup dictionary");
							case MissingKeyBehaviour.ReplaceWithFallback:
								// Instead of appending the replacement value directly as before,
								// append the next placeholder with the current placeholder index.
								// Add the actual replacement format item into the replacement values.
								resultBuilder.Append(IndexAndFormat());
								placeholderIndex++;
								replacementParams.Add(fallbackReplacementValue);
								break;
							case MissingKeyBehaviour.Ignore:
								resultBuilder.AppendWithEscapedBrackets(thisToken.SourceString, thisToken.StartIndex, thisToken.Length);
								break;
						}
					}
				}
			}

			// return the resultant string
			return FormattableStringFactory.Create(resultBuilder.ToString(), replacementParams.ToArray());
		}

		/// <summary>
		/// Tokenizes a named format string into a list of text and parameter tokens for later processing.
		/// </summary>
		/// <param name="formatString">The format string, containing keys like {foo}</param>
		/// <param name="openBraceChar">The character used to begin parameters</param>
		/// <param name="closeBraceChar">The character used to end parameters</param>
		/// <returns>A list of text and parameter tokens representing the input format string</returns>
		public static IEnumerable<FormatToken> Tokenize(string formatString, char openBraceChar = '{', char closeBraceChar = '}') {
			if(formatString == null) throw new ArgumentNullException($"{nameof(formatString)} cannot be null.");

			int currentTokenStart = 0;

			// start the state machine!

			bool insideBraces = false;

			int index = 0;
			while(index < formatString.Length) {
				if(!insideBraces) {
					// currently not inside a pair of braces in the format string
					if(formatString[index] == openBraceChar) {
						// check if the brace is escaped
						if(index < formatString.Length - 1 && formatString[index + 1] == openBraceChar) {
							// ESCAPED OPEN BRACE

							// we have hit an escaped open brace
							// return current normal text, as well as the first brace
							// implemented as yield return, this generates a IEnumerator state machine.
							yield return new FormatToken(TokenType.Text, formatString, currentTokenStart, (index - currentTokenStart) + 1);

							// skip over braces
							index += 2;

							// set new current token start and current token length
							currentTokenStart = index;

							continue;
						} else {
							// START OF PARAMETER

							// not an escaped brace, set state to inside brace
							insideBraces = true;

							// we are leaving standard text and entering into a parameter
							// add the text traversed so far as a text token
							if(currentTokenStart < index) {
								yield return new FormatToken(TokenType.Text, formatString, currentTokenStart, (index - currentTokenStart));
							}

							// set the start index of the token to the start of this parameter
							currentTokenStart = index;

							index++;

							continue;
						}
					} else if(formatString[index] == closeBraceChar) {
						// handle case where closing brace is encountered outside braces
						if(index < formatString.Length - 1 && formatString[index + 1] == closeBraceChar) {
							// this is an escaped closing brace, this is okay

							// add the current normal text, as well as the first brace, to the
							// list of tokens as a text token.
							yield return new FormatToken(TokenType.Text, formatString, currentTokenStart, (index - currentTokenStart) + 1);

							// skip over braces
							index += 2;

							// set new current token start and current token length
							currentTokenStart = index;

							continue;
						} else {
							// this is an unescaped closing brace outside of braces.
							// throw a format exception
							throw new FormatException($"Unexpected closing brace at position {index}");
						}
					} else {
						// move onto next character
						index++;
						continue;
					}
				} else {
					// currently inside a pair of braces in the format string
					if(formatString[index] == openBraceChar) {
						// found an opening brace
						// check if the brace is escaped
						if(index < formatString.Length - 1 && formatString[index + 1] == openBraceChar) {
							// there are escaped braces within the key
							// this is illegal, throw a format exception
							throw new FormatException($"Illegal escaped opening braces within a parameter at position {index}");
						} else {
							// not an escaped brace, we have an unexpected opening brace within a pair of braces
							throw new FormatException($"Unexpected opening brace inside a parameter at position {index}");
						}
					} else if(formatString[index] == closeBraceChar) {
						// END OF PARAMETER
						// handle case where closing brace is encountered inside braces
						// don't attempt to check for escaped braces here - always assume the first brace closes the braces
						// since we cannot have escaped braces within parameters.

						// Add the parameter information to the parameter list
						yield return new FormatToken(TokenType.Parameter, formatString, currentTokenStart, (index - currentTokenStart) + 1);

						// set the state to be outside of any braces
						insideBraces = false;

						// jump over brace
						index++;

						// update current token start
						currentTokenStart = index;

						// jump to next state
						continue;
					} // if }
					  else {
						// character has no special meaning, it is part of the current key
						// move onto next character
						index++;
						continue;
					} // else
				} // if inside brace
			} // while index < formatString.Length

			// after the loop, if all braces were balanced, we should be outside all braces
			// if we're not, the input string was misformatted.
			if(insideBraces) {
				throw new FormatException($"The format string ended before the parameter was closed. Position {index}");
			} else {
				// outside braces. Add on any remaining text at the end of the format string
				if(currentTokenStart < index) {
					yield return new FormatToken(TokenType.Text, formatString, currentTokenStart, index - currentTokenStart);
				}
			}

			// finished tokenizing, so yield break to make MoveNext return false on the IEnumerator
			yield break;
		}
	}
}
