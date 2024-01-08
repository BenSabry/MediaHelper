// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Helpers.CommonHelper.FormatNumberToLength(System.Int32,System.Int32)~System.String")]
[assembly: SuppressMessage("Blocker Code Smell", "S3877:Exceptions should not be thrown from unexpected methods", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Helpers.ExifHelper.#cctor")]
[assembly: SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
[assembly: SuppressMessage("Major Code Smell", "S6562:Always set the \"DateTimeKind\" when creating new \"DateTime\" instances", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Helpers.DateHelper.IsValidDateTime(System.Int64)~System.Boolean")]
[assembly: SuppressMessage("Minor Code Smell", "S2486:Generic exceptions should not be ignored", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Helpers.DateHelper.IsValidDateTime(System.Int64)~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Helpers.DateHelper.IsValidDateTime(System.Int64)~System.Boolean")]
[assembly: SuppressMessage("Major Code Smell", "S108:Nested blocks of code should not be left empty", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Helpers.DateHelper.IsValidDateTime(System.Int64)~System.Boolean")]
[assembly: SuppressMessage("Major Code Smell", "S6562:Always set the \"DateTimeKind\" when creating new \"DateTime\" instances", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Helpers.DateHelper.MatchNumericRegex(System.String)~System.ValueTuple{System.Boolean,System.DateTime}")]
[assembly: SuppressMessage("Minor Code Smell", "S6603:The collection-specific \"TrueForAll\" method should be used instead of the \"All\" extension", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Core.Engine.SourcesAreValidToUse~System.Boolean")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Core.Engine.TryMoveFile(MediaOrganizer.Helpers.LogHelper,System.IO.FileInfo@,System.String)")]
[assembly: SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Helpers.DateHelper.GetFormatedRegex~System.ValueTuple{System.Text.RegularExpressions.Regex,System.String}[]")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison for clarity", Justification = "<Pending>", Scope = "member", Target = "~M:MediaOrganizer.Helpers.DateHelper.RecoverOptionalCharsInFormatUsingValue(System.String,System.String)~System.String")]
