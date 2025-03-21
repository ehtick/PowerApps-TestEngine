﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.PowerFx;
using Microsoft.PowerApps.TestEngine.PowerFx.Functions;
using Microsoft.PowerApps.TestEngine.Providers;
using Microsoft.PowerApps.TestEngine.Providers.PowerFxModel;
using Microsoft.PowerApps.TestEngine.Tests.Helpers;
using Microsoft.PowerFx.Types;
using Moq;
using Xunit;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerFx.Functions
{
    public class SelectFunctionTests
    {
        private Mock<ITestWebProvider> MockTestWebProvider;
        private Mock<ITestState> MockTestState;
        private Mock<ILogger> MockLogger;

        public SelectFunctionTests()
        {
            MockTestWebProvider = new Mock<ITestWebProvider>(MockBehavior.Strict);
            MockTestState = new Mock<ITestState>(MockBehavior.Strict);
            MockLogger = new Mock<ILogger>(MockBehavior.Strict);
        }

        [Fact]
        public void SelectFunctionThrowsOnNullObjectTest()
        {
            var selectOneParamFunction = new SelectOneParamFunction(MockTestWebProvider.Object, () => Task.CompletedTask, MockLogger.Object);
            var selectTwoParamsFunction = new SelectTwoParamsFunction(MockTestWebProvider.Object, () => Task.CompletedTask, MockLogger.Object);
            var selectThreeParamsFunction = new SelectThreeParamsFunction(MockTestWebProvider.Object, () => Task.CompletedTask, MockLogger.Object);

            Assert.ThrowsAny<Exception>(() => selectOneParamFunction.Execute(null));
            Assert.ThrowsAny<Exception>(() => selectTwoParamsFunction.Execute(null, null));
            Assert.ThrowsAny<Exception>(() => selectThreeParamsFunction.Execute(null, null, null));
        }

        [Fact]
        public void SelectFunctionThrowsOnNonPowerAppsRecordValuetTest()
        {
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);
            var selectOneParamFunction = new SelectOneParamFunction(MockTestWebProvider.Object, () => Task.CompletedTask, MockLogger.Object);
            var selectTwoParamsFunction = new SelectTwoParamsFunction(MockTestWebProvider.Object, () => Task.CompletedTask, MockLogger.Object);
            var selectThreeParamsFunction = new SelectThreeParamsFunction(MockTestWebProvider.Object, () => Task.CompletedTask, MockLogger.Object);

            var someOtherRecordValue = new SomeOtherRecordValue(recordType);
            Assert.ThrowsAny<Exception>(() => selectOneParamFunction.Execute(someOtherRecordValue));
            Assert.ThrowsAny<Exception>(() => selectTwoParamsFunction.Execute(someOtherRecordValue, It.IsAny<NumberValue>()));
            Assert.ThrowsAny<Exception>(() => selectThreeParamsFunction.Execute(someOtherRecordValue, It.IsAny<NumberValue>(), It.IsAny<RecordValue>()));
        }

        [Fact]
        public void SelectOneParamFunctionTest()
        {
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(true));
            LoggingTestHelper.SetupMock(MockLogger);
            var recordType = RecordType.Empty().Add("Text", FormulaType.String);

            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Button1");

            var updaterFunctionCallCount = 0;
            var updaterFunction = () =>
            {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectOneParamFunction(MockTestWebProvider.Object, updaterFunction, MockLogger.Object);

            var result = selectFunction.Execute(recordValue);
            Assert.IsType<BlankValue>(result);
            MockTestWebProvider.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), null), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n\n" + "Executing Select function.", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "Successfully finished executing Select function.", LogLevel.Information, Times.Once());
            Assert.Equal(1, updaterFunctionCallCount);
        }

        [Fact]
        public void SelectTwoParamFunctionTest()
        {
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(true));
            LoggingTestHelper.SetupMock(MockLogger);
            var recordType = RecordType.Empty().Add("Gallery1", RecordType.Empty());

            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Gallery1");
            var rowOrColumn = NumberValue.New(1.0);

            var updaterFunctionCallCount = 0;
            var updaterFunction = () =>
            {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectTwoParamsFunction(MockTestWebProvider.Object, updaterFunction, MockLogger.Object);

            var result = selectFunction.Execute(recordValue, rowOrColumn);
            Assert.IsType<BlankValue>(result);
            MockTestWebProvider.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), null), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n\n" + "Executing Select function.", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "Successfully finished executing Select function.", LogLevel.Information, Times.Once());
            Assert.Equal(1, updaterFunctionCallCount);
        }

        [Fact]
        public void SelectThreeParamFunctionTest()
        {
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(true));
            LoggingTestHelper.SetupMock(MockLogger);
            var parentRecordType = RecordType.Empty().Add("Gallery1", RecordType.Empty());
            var childRecordType = RecordType.Empty().Add("Button1", RecordType.Empty());

            var parentValue = new ControlRecordValue(parentRecordType, MockTestWebProvider.Object, "Gallery1");
            var rowOrColumn = NumberValue.New(1.0);
            var childValue = new ControlRecordValue(childRecordType, MockTestWebProvider.Object, "Button1");

            var updaterFunctionCallCount = 0;
            var updaterFunction = () =>
            {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectThreeParamsFunction(MockTestWebProvider.Object, updaterFunction, MockLogger.Object);

            var result = selectFunction.Execute(parentValue, rowOrColumn, childValue);
            Assert.IsType<BlankValue>(result);
            MockTestWebProvider.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == childValue.Name), null), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n\n" + "Executing Select function.", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "Successfully finished executing Select function.", LogLevel.Information, Times.Once());
            Assert.Equal(1, updaterFunctionCallCount);
        }

        [Fact]
        public void SelectOneParamFunctionFailsTest()
        {
            LoggingTestHelper.SetupMock(MockLogger);
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(false));

            var recordType = RecordType.Empty().Add("Text", FormulaType.String);

            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Button1");

            var updaterFunctionCallCount = 0;
            var updaterFunction = () =>
            {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectOneParamFunction(MockTestWebProvider.Object, updaterFunction, MockLogger.Object);

            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(recordValue));
            MockTestWebProvider.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), null), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n\n" + "Executing Select function.", LogLevel.Information, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "Control name: Button1", LogLevel.Trace, Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "Unable to select control", LogLevel.Error, Times.Once());
            Assert.Equal(0, updaterFunctionCallCount);
        }

        [Fact]
        public void SelectTwoParamFunctionFailsTest()
        {
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(false));
            LoggingTestHelper.SetupMock(MockLogger);
            var recordType = RecordType.Empty().Add("Gallery1", RecordType.Empty());

            var recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Gallery1");
            var rowOrColumn = NumberValue.New(1.0);

            var updaterFunctionCallCount = 0;
            var updaterFunction = () =>
            {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectTwoParamsFunction(MockTestWebProvider.Object, updaterFunction, MockLogger.Object);

            // Testing Scenarios where members(control, row or column) are null
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(null, rowOrColumn));
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(recordValue, null));

            // Adding test where control names are null
            recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, null);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(recordValue, rowOrColumn));

            recordValue = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Gallery1");
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(recordValue, rowOrColumn));
            MockTestWebProvider.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == recordValue.Name), null), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n\n" + "Executing Select function.", LogLevel.Information, Times.AtLeastOnce());
            LoggingTestHelper.VerifyLogging(MockLogger, "Control name: Gallery1", LogLevel.Trace, Times.AtLeastOnce());
            LoggingTestHelper.VerifyLogging(MockLogger, "Unable to select control", LogLevel.Error, Times.AtLeastOnce());
            Assert.Equal(0, updaterFunctionCallCount);
        }

        [Fact]
        public void SelectThreeParamFunctionFailsTest()
        {
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(false));
            LoggingTestHelper.SetupMock(MockLogger);
            var parentRecordType = RecordType.Empty().Add("Gallery1", RecordType.Empty());
            var childRecordType = RecordType.Empty().Add("Button1", RecordType.Empty());

            var parentValue = new ControlRecordValue(parentRecordType, MockTestWebProvider.Object, "Gallery1");
            var rowOrColumn = NumberValue.New(1.0);
            var childValue = new ControlRecordValue(childRecordType, MockTestWebProvider.Object, "Button1");

            var updaterFunctionCallCount = 0;
            var updaterFunction = () =>
            {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };
            var selectFunction = new SelectThreeParamsFunction(MockTestWebProvider.Object, updaterFunction, MockLogger.Object);

            // Testing Scenarios where members(parent control, row or column and child control) are null
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(null, rowOrColumn, childValue));
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(parentValue, null, childValue));
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(parentValue, rowOrColumn, null));

            // Adding test where control names are null
            parentValue = new ControlRecordValue(parentRecordType, MockTestWebProvider.Object, null);
            childValue = new ControlRecordValue(childRecordType, MockTestWebProvider.Object, null);
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(parentValue, rowOrColumn, childValue));

            parentValue = new ControlRecordValue(parentRecordType, MockTestWebProvider.Object, "Gallery1");
            childValue = new ControlRecordValue(childRecordType, MockTestWebProvider.Object, "Button1");
            Assert.ThrowsAny<Exception>(() => selectFunction.Execute(parentValue, rowOrColumn, childValue));
            MockTestWebProvider.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == childValue.Name), null), Times.Once());
            LoggingTestHelper.VerifyLogging(MockLogger, "------------------------------\n\n" + "Executing Select function.", LogLevel.Information, Times.AtLeastOnce());
            LoggingTestHelper.VerifyLogging(MockLogger, "Control name: Button1", LogLevel.Trace, Times.AtLeastOnce());
            LoggingTestHelper.VerifyLogging(MockLogger, "Unable to select control", LogLevel.Error, Times.AtLeastOnce());
            Assert.Equal(0, updaterFunctionCallCount);
        }

        [Fact]
        public void SelectGalleryTest()
        {
            MockTestWebProvider.Setup(x => x.SelectControlAsync(It.IsAny<ItemPath>(), null)).Returns(Task.FromResult(true));
            LoggingTestHelper.SetupMock(MockLogger);
            var parentRecordType = RecordType.Empty().Add("Gallery1", RecordType.Empty());
            var childRecordType = RecordType.Empty().Add("Button1", RecordType.Empty());

            var updaterFunctionCallCount = 0;
            var updaterFunction = () =>
            {
                updaterFunctionCallCount++;
                return Task.CompletedTask;
            };

            // Select gallery item using oneparam select function
            // `Select(Index(Gallery1.AllItems, 1).Button1);`
            var parentItemPath = new ItemPath()
            {
                ControlName = "Gallery1",
                Index = 0,
                ParentControl = null,
                PropertyName = "AllItems"
            };
            var itemPath = new ItemPath()
            {
                ControlName = "Button1",
                Index = null,
                ParentControl = parentItemPath,
                PropertyName = null
            };

            var recordType = RecordType.Empty().Add("Button1", RecordType.Empty());
            var powerAppControlModel = new ControlRecordValue(recordType, MockTestWebProvider.Object, "Button1", parentItemPath);

            var selectFunction = new SelectOneParamFunction(MockTestWebProvider.Object, updaterFunction, MockLogger.Object);
            var result = selectFunction.Execute(powerAppControlModel);
            Assert.IsType<BlankValue>(result);

            // Select gallery item using threeparams select function
            // `Select(Gallery1, 1, Button1);`
            var parentValue = new ControlRecordValue(parentRecordType, MockTestWebProvider.Object, "Gallery1");
            var rowOrColumn = NumberValue.New(1.0);
            var childValue = new ControlRecordValue(childRecordType, MockTestWebProvider.Object, "Button1");

            var selectthreeParamsFunction = new SelectThreeParamsFunction(MockTestWebProvider.Object, updaterFunction, MockLogger.Object);
            result = selectthreeParamsFunction.Execute(parentValue, rowOrColumn, childValue);
            Assert.IsType<BlankValue>(result);

            MockTestWebProvider.Verify(x => x.SelectControlAsync(It.Is<ItemPath>((item) => item.ControlName == childValue.Name), null), Times.Exactly(2));
            Assert.Equal(2, updaterFunctionCallCount);
        }
    }
}
