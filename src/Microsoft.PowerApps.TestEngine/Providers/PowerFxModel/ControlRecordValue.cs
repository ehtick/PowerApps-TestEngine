﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Helpers;
using Microsoft.PowerFx.Types;
using Newtonsoft.Json;

namespace Microsoft.PowerApps.TestEngine.Providers.PowerFxModel
{
    /// <summary>
    /// This is a Power FX RecordValue created to represent a control or a control property
    /// </summary>
    public class ControlRecordValue : RecordValue
    {
        private readonly ITestWebProvider _testWebProvider;
        private readonly string _name;
        private readonly ItemPath _parentItemPath;

        /// <summary>
        /// Creates a ControlRecordValue
        /// </summary>
        /// <param name="type">Record type for the control record value</param>
        /// <param name="testWebProvider">Power App functions so that the property values can be fetched</param>
        /// <param name="logger">Our logger object</param>
        /// <param name="name">Name of the control</param>
        /// <param name="parentItemPath">Path to the parent control</param>
        public ControlRecordValue(RecordType type, ITestWebProvider testWebProvider, string name = null, ItemPath parentItemPath = null) : base(type)
        {
            _testWebProvider = testWebProvider;
            _parentItemPath = parentItemPath;
            _name = name;
        }

        /// <summary>
        /// Gets the name of the control
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Gets the path to the control that can be used by the javascript
        /// </summary>
        /// <param name="propertyName">Property name. Optional</param>
        /// <returns>Path to control</returns>
        public ItemPath GetItemPath(string propertyName = null)
        {
            return new ItemPath()
            {
                ControlName = _name,
                PropertyName = propertyName,
                ParentControl = _parentItemPath
            };
        }

        /// <summary>
        /// Gets a field from the control.
        /// </summary>
        /// <param name="fieldType">Type of the field</param>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="result">Value of the field</param>
        /// <returns>True if able to get the field value</returns>
        protected override bool TryGetField(FormulaType fieldType, string fieldName, out FormulaValue result)
        {

            if (fieldType is TableType)
            {
                // This would be if we were referencing a property that could be indexed. Eg. Gallery1.AllItems (fieldName = AllItems)
                var tableType = fieldType as TableType;
                var recordType = tableType.ToRecord();
                // Create indexable table source
                var tableSource = new ControlTableSource(_testWebProvider, GetItemPath(fieldName), recordType);
                var table = new ControlTableValue(recordType, tableSource, _testWebProvider);
                result = table;
                return true;
            }
            else if (fieldType is RecordType)
            {
                var recordType = fieldType as RecordType;
                if (string.IsNullOrEmpty(_name))
                {
                    // We reach here if we are referencing a child item in a Gallery. Eg. Index(Gallery1.AllItems).Label1 (fieldName = Label1)
                    result = new ControlRecordValue(recordType, _testWebProvider, fieldName, _parentItemPath);
                    return true;
                }
                else
                {
                    // We reach here if we are referencing a child item in a component. Eg. Component1.Label1 (fieldName = Label1)
                    result = new ControlRecordValue(recordType, _testWebProvider, fieldName, GetItemPath());
                    return true;
                }
            }
            else
            {
                // We reach here if we are referencing a terminating property of a control, Eg. Label1.Text (fieldName = Text)
                var itemPath = GetItemPath(fieldName);

                var propertyValueJson = _testWebProvider.GetPropertyValueFromControl<string>(itemPath);

                if (string.IsNullOrEmpty(propertyValueJson))
                {
                    result = BlankValue.NewBlank(fieldType);
                    return true;
                }

                var jsPropertyValueModel = JsonConvert.DeserializeObject<JSPropertyValueModel>(propertyValueJson);

                if (jsPropertyValueModel != null)
                {
                    if (string.IsNullOrEmpty(jsPropertyValueModel.PropertyValue) && fieldType is not StringType)
                    {
                        result = null;
                        return false;
                    }

                    if (fieldType is NumberType)
                    {
                        result = NumberValue.New(double.Parse(jsPropertyValueModel.PropertyValue));
                        return true;
                    }
                    else if (fieldType is DecimalType)
                    {
                        result = DecimalValue.New(decimal.Parse(jsPropertyValueModel.PropertyValue));
                        return true;
                    }
                    else if (fieldType is BooleanType)
                    {
                        result = BooleanValue.New(bool.Parse(jsPropertyValueModel.PropertyValue));
                        return true;
                    }
                    else if (fieldType is GuidType)
                    {
                        result = GuidValue.New(new Guid(jsPropertyValueModel.PropertyValue));
                        return true;
                    }
                    else if (fieldType is DateTimeType)
                    {
                        long milliseconds;

                        // When converted from DateTime to a string, a value from Wait() gets roundtripped into a UTC Timestamp format
                        // The compiler does not register this format as a valid DateTime format
                        // Because of this, we have to manually convert it into a DateTime
                        if (long.TryParse(jsPropertyValueModel.PropertyValue, out milliseconds))
                        {
                            var trueDateTime = new DateTime(1970, 1, 1, 0, 0, 0).AddMilliseconds(milliseconds);
                            result = DateTimeValue.New(trueDateTime);
                        }
                        // When converted from DateTime to a string, a value from SetProperty() retains it's MMDDYYYY hh::mm::ss format
                        // This allows us to just parse it back into a datetime, without having to manually convert it back
                        else
                        {
                            result = DateTimeValue.New(DateTime.Parse(jsPropertyValueModel.PropertyValue));
                        }

                        return true;
                    }
                    else if (fieldType is DateType)
                    {
                        long milliseconds;

                        // When converted from Date to a string, a value from Wait() gets roudntripped into a UTC Timestamp format
                        // The compiler does not register this format as a valid DateTime format
                        // Because of this, we have to manually convert it into a DateTime
                        if (long.TryParse(jsPropertyValueModel.PropertyValue, out milliseconds))
                        {
                            var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
                            DateTime trueDateTime = dateTimeOffset.LocalDateTime;
                            result = DateValue.NewDateOnly(trueDateTime.Date);
                        }
                        // When converted from DateTime to a string, a value from SetProperty() retains it's MMDDYYYY hh::mm::ss format
                        // This allows us to just parse it back into a DateTime, without having to manually convert it back
                        // We then use said DateTime to create the DateValue
                        else
                        {
                            result = DateValue.NewDateOnly(DateTime.Parse(jsPropertyValueModel.PropertyValue));
                        }

                        return true;
                    }

                    result = New(jsPropertyValueModel.PropertyValue);
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
