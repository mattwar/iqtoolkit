// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.IO;

namespace IQToolkit.Utils
{
    /// <summary>
    /// A writer for writing text with line indenation.
    /// </summary>
    public class IndentWriter
    {
        private readonly TextWriter _writer;
        private readonly string _indentation;
        private string _currentIndentation;
        private bool _startOfLine;

        public IndentWriter(TextWriter writer, string indentation)
        {
            _writer = writer;
            _indentation = indentation;
            _currentIndentation = "";
            _startOfLine = true;
        }

        /// <summary>
        /// Write the text.
        /// </summary>
        public void Write(string text)
        {
            if (_startOfLine)
            {
                _writer.Write(_currentIndentation);
                if (_currentIndentation.Length > 0)
                    _startOfLine = false;
            }

            _writer.Write(text);

            if (text.Length > 0)
                _startOfLine = false;
        }

        /// <summary>
        /// Writes the text indented.
        /// The indentation only occurs if the text is the first text written after a new line.
        /// </summary>
        public void WriteIndented(string text)
        {
            WriteIndented(() => Write(text));
        }

        /// <summary>
        /// Executes the action with writing indented one level deeper.
        /// </summary>
        public void WriteIndented(Action action)
        {
            var oldIndentation = _currentIndentation;
            _currentIndentation += _indentation;
            action();
            _currentIndentation = oldIndentation;
        }

        /// <summary>
        /// Writes a new line conditionally.
        /// </summary>
        public void WriteLine(bool allowBlankLines = false)
        {
            if (!_startOfLine || allowBlankLines)
            {
                _writer.WriteLine();
                _startOfLine = true;
            }
        }

        /// <summary>
        /// Write a blank line.
        /// </summary>
        public void WriteBlankLine()
        {
            this.WriteLine(false);
            this.WriteLine(true);
        }

        /// <summary>
        /// Writes the text and then a new line.
        /// </summary>
        public void WriteLine(string text)
        {
            this.Write(text);
            this.WriteLine();
        }

        /// <summary>
        /// Writes the list of items separated by the separator.
        /// </summary>
        public void WriteSeparated<T>(
            IReadOnlyList<T> items,
            string seperator,
            Action<T> fnWriteItem)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                    this.Write(seperator);
                fnWriteItem(items[i]);
            }
        }

        /// <summary>
        /// Writes the list of items separated by a separator.
        /// </summary>
        public void WriteSeparated<T>(
            IReadOnlyList<T> items,
            Action<T> fnWriteItem,
            Action fnWriteSeparator)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                    fnWriteSeparator();
                fnWriteItem(items[i]);
            }
        }

        public void WriteLineSeparated<T>(
            IReadOnlyList<T> items,
            Action<T> fnWriteItem)
        {
            WriteSeparated(
                items,
                fnWriteItem,
                () => this.WriteLine()
                );
        }

        public void WriteBlankLineSeparated<T>(
            IReadOnlyList<T> items,
            Action<T> fnWriteItem)
        {
            WriteSeparated(
                items,
                fnWriteItem,
                () => this.WriteBlankLine()
                );
        }

        /// <summary>
        /// Writes the list of items with a comma separator.
        /// </summary>
        public void WriteCommaSeparated<T>(
            IReadOnlyList<T> items, 
            Action<T> fnWriteItem)
        {
            WriteSeparated(items, ", ", fnWriteItem);
        }
    }
}