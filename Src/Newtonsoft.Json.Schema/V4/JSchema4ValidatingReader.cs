﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema.Infrastructure;
using Newtonsoft.Json.Schema.V4.Infrastructure.Validation;
using Newtonsoft.Json.Utilities;

namespace Newtonsoft.Json.Schema.V4
{
    /// <summary>
    /// Represents a reader that provides <see cref="JSchema"/> validation.
    /// </summary>
    public class JSchema4ValidatingReader : JsonReader, IJsonLineInfo
    {
        internal class ReaderValidator : Validator
        {
            private readonly JSchema4ValidatingReader _reader;

            public ReaderValidator(JSchema4ValidatingReader reader)
                : base(reader)
            {
                _reader = reader;
            }

            public override ISchemaError CreateError(string message, JSchema4 schema, IList<ISchemaError> childErrors)
            {
                return CreateError(message, schema, childErrors, _reader, _reader.Path);
            }
        }

        private readonly JsonReader _reader;
        private readonly ReaderValidator _validator;

        /// <summary>
        /// Sets an event handler for receiving schema validation errors.
        /// </summary>
        public event SchemaValidationEventHandler ValidationEventHandler
        {
            add { _validator.ValidationEventHandler += value; }
            remove { _validator.ValidationEventHandler -= value; }
        }

        /// <summary>
        /// Gets the text value of the current JSON token.
        /// </summary>
        /// <value></value>
        public override object Value
        {
            get { return _reader.Value; }
        }

        /// <summary>
        /// Gets the depth of the current token in the JSON document.
        /// </summary>
        /// <value>The depth of the current token in the JSON document.</value>
        public override int Depth
        {
            get { return _reader.Depth; }
        }

        /// <summary>
        /// Gets the path of the current JSON token. 
        /// </summary>
        public override string Path
        {
            get { return _reader.Path; }
        }

        /// <summary>
        /// Gets the quotation mark character used to enclose the value of a string.
        /// </summary>
        /// <value></value>
        public override char QuoteChar
        {
            get { return _reader.QuoteChar; }
        }

        /// <summary>
        /// Gets the type of the current JSON token.
        /// </summary>
        /// <value></value>
        public override JsonToken TokenType
        {
            get { return _reader.TokenType; }
        }

        /// <summary>
        /// Gets the Common Language Runtime (CLR) type for the current JSON token.
        /// </summary>
        /// <value></value>
        public override Type ValueType
        {
            get { return _reader.ValueType; }
        }

        /// <summary>
        /// Gets or sets the schema.
        /// </summary>
        /// <value>The schema.</value>
        public JSchema4 Schema
        {
            get { return _validator.Schema; }
            set
            {
                if (TokenType != JsonToken.None)
                    throw new InvalidOperationException("Cannot change schema while validating JSON.");

                _validator.Schema = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="JsonReader"/> used to construct this <see cref="JSchemaValidatingReader"/>.
        /// </summary>
        /// <value>The <see cref="JsonReader"/> specified in the constructor.</value>
        public JsonReader Reader
        {
            get { return _reader; }
        }

        bool IJsonLineInfo.HasLineInfo()
        {
            IJsonLineInfo lineInfo = _reader as IJsonLineInfo;
            return lineInfo != null && lineInfo.HasLineInfo();
        }

        int IJsonLineInfo.LineNumber
        {
            get
            {
                IJsonLineInfo lineInfo = _reader as IJsonLineInfo;
                return (lineInfo != null) ? lineInfo.LineNumber : 0;
            }
        }

        int IJsonLineInfo.LinePosition
        {
            get
            {
                IJsonLineInfo lineInfo = _reader as IJsonLineInfo;
                return (lineInfo != null) ? lineInfo.LinePosition : 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JSchemaValidatingReader"/> class that
        /// validates the content returned from the given <see cref="JsonReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from while validating.</param>
        public JSchema4ValidatingReader(JsonReader reader)
        {
            ValidationUtils.ArgumentNotNull(reader, "reader");
            _reader = reader;
            _validator = new ReaderValidator(this);
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="Nullable{Int32}"/>.
        /// </summary>
        /// <returns>A <see cref="Nullable{Int32}"/>.</returns>
        public override int? ReadAsInt32()
        {
            int? i = _reader.ReadAsInt32();

            ValidateCurrentToken();
            return i;
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="Byte"/>[].
        /// </summary>
        /// <returns>
        /// A <see cref="Byte"/>[] or a null reference if the next JSON token is null.
        /// </returns>
        public override byte[] ReadAsBytes()
        {
            byte[] data = _reader.ReadAsBytes();

            ValidateCurrentToken();
            return data;
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="Nullable{Decimal}"/>.
        /// </summary>
        /// <returns>A <see cref="Nullable{Decimal}"/>.</returns>
        public override decimal? ReadAsDecimal()
        {
            decimal? d = _reader.ReadAsDecimal();

            ValidateCurrentToken();
            return d;
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="String"/>.
        /// </summary>
        /// <returns>A <see cref="String"/>. This method will return <c>null</c> at the end of an array.</returns>
        public override string ReadAsString()
        {
            string s = _reader.ReadAsString();

            ValidateCurrentToken();
            return s;
        }

        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="Nullable{DateTime}"/>.
        /// </summary>
        /// <returns>A <see cref="String"/>. This method will return <c>null</c> at the end of an array.</returns>
        public override DateTime? ReadAsDateTime()
        {
            DateTime? dateTime = _reader.ReadAsDateTime();

            ValidateCurrentToken();
            return dateTime;
        }

#if !NET20
        /// <summary>
        /// Reads the next JSON token from the stream as a <see cref="Nullable{DateTimeOffset}"/>.
        /// </summary>
        /// <returns>A <see cref="Nullable{DateTimeOffset}"/>.</returns>
        public override DateTimeOffset? ReadAsDateTimeOffset()
        {
            DateTimeOffset? dateTimeOffset = _reader.ReadAsDateTimeOffset();

            ValidateCurrentToken();
            return dateTimeOffset;
        }
#endif

        /// <summary>
        /// Reads the next JSON token from the stream.
        /// </summary>
        /// <returns>
        /// true if the next token was read successfully; false if there are no more tokens to read.
        /// </returns>
        public override bool Read()
        {
            if (!_reader.Read())
                return false;

            if (_reader.TokenType == JsonToken.Comment)
                return true;

            ValidateCurrentToken();
            return true;
        }

        private void ValidateCurrentToken()
        {
            _validator.ValidateCurrentToken(_reader.TokenType, _reader.Value, _reader.Depth);
        }
    }
}