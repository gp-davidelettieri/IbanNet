﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IbanNet.Extensions;
using IbanNet.Validation.Results;

namespace IbanNet
{
	/// <summary>
	/// Represents an IBAN.
	/// </summary>
	[TypeConverter(typeof(TypeConverters.IbanTypeConverter))]
	public sealed class Iban
	{
		/// <summary>
		/// The supported IBAN output formats.
		/// </summary>
		public static class Formats
		{
			/// <summary>
			/// Partitions an IBAN into 4 character segments separated with a space.
			/// </summary>
			public const string Partitioned = "S";

			/// <summary>
			/// An IBAN without whitespace.
			/// </summary>
			public const string Flat = "F";
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly string _iban;

		/// <summary>
		/// Gets or sets the <see cref="IIbanValidator"/> used to validate an IBAN.
		/// </summary>
		// ReSharper disable once MemberCanBePrivate.Global
		public static IIbanValidator Validator { get; set; } = new IbanValidator();

		private Iban(string iban)
		{
			_iban = iban;
		}

		/// <summary>Returns a string that represents the current <see cref="Iban"/>.</summary>
		/// <example>
		/// F => NL91ABNA0417164300
		/// S => NL91 ABNA 0417 1643 00
		/// </example>
		/// <param name="format">The format to use. F = flat, S = partitioned by space.</param>
		/// <returns>A string that represents the current <see cref="Iban"/>.</returns>
		public string ToString(string format)
		{
			switch (format)
			{
				// Flat
				case Formats.Flat:
					return _iban;

				// Partitioned by space
				case Formats.Partitioned:
					// Split into 4 char segments.
					IEnumerable<string> segments = _iban.Partition(4).Select(p => string.Join("", p));
					return string.Join(" ", segments);

				case null:
					throw new ArgumentNullException(nameof(format), string.Format(Resources.The_format_is_required_with_supported_formats, Formats.Flat, Formats.Partitioned));

				default:
					throw new ArgumentException(string.Format(Resources.The_format_0_is_invalid_with_supported_formats, format, Formats.Flat, Formats.Partitioned), nameof(format));
			}
		}

		/// <summary>Returns a string that represents the current <see cref="Iban"/>.</summary>
		/// <returns>A string that represents the current <see cref="Iban"/>.</returns>
		public override string ToString()
		{
			return ToString(Formats.Flat);
		}

		/// <summary>
		/// Parses the specified <paramref name="value"/> into an <see cref="Iban"/>.
		/// </summary>
		/// <param name="value">The IBAN value to parse.</param>
		/// <returns>an <see cref="Iban"/> if the <paramref name="value"/> is parsed successfully</returns>
		/// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="value"/> is null.</exception>
		/// <exception cref="IbanFormatException">Thrown when the specified <paramref name="value"/> is not a valid IBAN.</exception>
		public static Iban Parse(string? value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (TryParse(value, out Iban? iban, out ValidationResult? validationResult, out Exception? exceptionThrown))
			{
				return iban;
			}

			string errorMessage = !(validationResult?.Result is ErrorResult errorResult) || string.IsNullOrEmpty(errorResult.ErrorMessage)
				? string.Format(Resources.The_value_0_is_not_a_valid_IBAN, value)
				: errorResult.ErrorMessage;

			throw new IbanFormatException(errorMessage, validationResult, exceptionThrown);
		}

		/// <summary>
		/// Attempts to parse the specified <paramref name="value"/> into an <see cref="Iban"/>.
		/// </summary>
		/// <param name="value">The IBAN value to parse.</param>
		/// <param name="iban">The <see cref="Iban"/> if the <paramref name="value"/> is parsed successfully.</param>
		/// <returns>true if the <paramref name="value"/> is parsed successfully, or false otherwise</returns>
		public static bool TryParse(string? value, [NotNullWhen(true)] out Iban? iban)
		{
			return TryParse(value, out iban, out _, out _);
		}

		/// <summary>
		/// Attempts to parse the specified <paramref name="value"/> into an <see cref="Iban"/>.
		/// </summary>
		/// <param name="value">The IBAN value to parse.</param>
		/// <param name="iban">The <see cref="Iban"/> if the <paramref name="value"/> is parsed successfully.</param>
		/// <param name="validationResult">The validation result.</param>
		/// <param name="exceptionThrown">The exception if one was thrown.</param>
		/// <returns>true if the <paramref name="value"/> is parsed successfully, or false otherwise</returns>
		internal static bool TryParse(
			string? value,
			[NotNullWhen(true)] out Iban? iban,
			out ValidationResult? validationResult,
			[MaybeNullWhen(false)] out Exception? exceptionThrown)
		{
			iban = null;
			exceptionThrown = null;

			// Although our validator normalizes too, we can't rely on this fact if other implementations
			// are provided (like mocks, or maybe faster validators). Thus, to ensure this class correctly
			// represents the IBAN value, we normalize inline here and take the penalty.
			string? normalizedValue = value.StripWhitespaceOrNull();
			try
			{
				validationResult = Validator.Validate(normalizedValue);
			}
			catch (Exception ex)
			{
				validationResult = null;
				exceptionThrown = ex;
				return false;
			}

			if (validationResult.IsValid)
			{
				iban = new Iban(normalizedValue!.ToUpperInvariant());
				return true;
			}

			return false;
		}

		private bool Equals(Iban other)
		{
			return string.Equals(_iban, other._iban);
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <param name="obj">The object to compare with the current object. </param>
		/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return obj is Iban iban && Equals(iban);
		}

		/// <summary>
		/// Serves as the default hash function.
		/// </summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			return _iban.GetHashCode();
		}

		/// <summary>
		/// Determines whether the <see cref="Iban"/>s are equal to each other.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(Iban left, Iban right)
		{
			return Equals(left, right);
		}

		/// <summary>
		/// Determines whether the <see cref="Iban"/>s are unequal to each other.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(Iban left, Iban right)
		{
			return !Equals(left, right);
		}
	}
}
