using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow.Generator.CodeDom;
using TechTalk.SpecFlow.Utils;

namespace TechTalk.SpecFlow.Generator.UnitTestProvider
{
    public class XUnitTestGeneratorProvider : IUnitTestGeneratorProvider
    {
        protected const string FEATURE_TITLE_PROPERTY_NAME = "FeatureTitle";
        protected const string DESCRIPTION_PROPERTY_NAME = "Description";
        protected const string FACT_ATTRIBUTE = "Xunit.FactAttribute";
        protected const string FACT_ATTRIBUTE_SKIP_PROPERTY_NAME = "Skip";
        protected internal const string THEORY_ATTRIBUTE = "Xunit.Extensions.TheoryAttribute";
        protected internal const string THEORY_ATTRIBUTE_SKIP_PROPERTY_NAME = "Skip";
        protected const string INLINEDATA_ATTRIBUTE = "Xunit.Extensions.InlineDataAttribute";
        protected internal const string SKIP_REASON = "Ignored";
        protected const string TRAIT_ATTRIBUTE = "Xunit.TraitAttribute";
        protected const string IUSEFIXTURE_INTERFACE = "Xunit.IUseFixture";
        protected const string CATEGORY_PROPERTY_NAME = "Category";
        protected const string IGNORE_TAG = "@Ignore";
        protected const string IASYNCLIFETIME_INTERFACE = "Xunit.IAsyncLifetime";

        private CodeTypeDeclaration _currentFixtureDataTypeDeclaration = null;

        protected CodeDomHelper CodeDomHelper { get; set; }

        public virtual UnitTestGeneratorTraits GetTraits()
        {
            return UnitTestGeneratorTraits.RowTests;
        }

        public bool GenerateParallelCodeForFeature { get; set; }

        public XUnitTestGeneratorProvider(CodeDomHelper codeDomHelper)
        {
            CodeDomHelper = codeDomHelper;
        }

        public void SetTestClass(TestClassGenerationContext generationContext, string featureTitle, string featureDescription)
        {
            // xUnit does not use an attribute for the TestFixture, all public classes are potential fixtures
        }

        public virtual void SetTestClassCategories(TestClassGenerationContext generationContext, IEnumerable<string> featureCategories)
        {
            // Set Category trait which can be used with the /trait or /-trait xunit flags to include/exclude tests
            foreach (string str in featureCategories)
                SetProperty(generationContext.TestClass, CATEGORY_PROPERTY_NAME, str);
        }

        public virtual void SetTestClassParallelize(TestClassGenerationContext generationContext)
        {

        }

        public void SetTestClassInitializeMethod(TestClassGenerationContext generationContext)
        {
            // xUnit uses IUseFixture<T> on the class

            generationContext.TestClassInitializeMethod.Attributes |= MemberAttributes.Static;
            generationContext.TestRunnerField.Attributes |= MemberAttributes.Static;

            _currentFixtureDataTypeDeclaration = CodeDomHelper.CreateGeneratedTypeDeclaration("FixtureData");
            generationContext.TestClass.Members.Add(_currentFixtureDataTypeDeclaration);

            var fixtureDataType =
                CodeDomHelper.CreateNestedTypeReference(generationContext.TestClass, _currentFixtureDataTypeDeclaration.Name);

            var useFixtureType = CreateFixtureInterface(generationContext, fixtureDataType);

            CodeDomHelper.SetTypeReferenceAsInterface(useFixtureType);

            generationContext.TestClass.BaseTypes.Add(useFixtureType);

            var asyncLifetimeType = new CodeTypeReference(IASYNCLIFETIME_INTERFACE);

            CodeDomHelper.SetTypeReferenceAsInterface(asyncLifetimeType);

            _currentFixtureDataTypeDeclaration.BaseTypes.Add(asyncLifetimeType);

            // Task IAsyncLifetime.InitializeAsync() { <fixtureSetupMethod>(); }
            var initializeMethod = new CodeMemberMethod();
            initializeMethod.PrivateImplementationType = new CodeTypeReference(IASYNCLIFETIME_INTERFACE);
            initializeMethod.Name = "InitializeAsync";
            initializeMethod.ReturnType = new CodeTypeReference(typeof(Task));

            CodeDomHelper.MarkCodeMemberMethodAsAsync(initializeMethod);

            var expression = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(new CodeTypeReference(generationContext.TestClass.Name)),
                generationContext.TestClassInitializeMethod.Name);

            CodeDomHelper.MarkCodeMethodInvokeExpressionAsAwait(expression);

            _currentFixtureDataTypeDeclaration.Members.Add(initializeMethod);
            initializeMethod.Statements.Add(
                expression);
        }

        public virtual bool ImplmentInterfaceExplicit => true;

        public void SetTestClassCleanupMethod(TestClassGenerationContext generationContext)
        {
            // xUnit uses IUseFixture<T> on the class

            generationContext.TestClassCleanupMethod.Attributes |= MemberAttributes.Static;

            // xUnit supports test tear down through the IAsyncLifetime interface

            var asyncLifetimeType = new CodeTypeReference(IASYNCLIFETIME_INTERFACE);

            CodeDomHelper.SetTypeReferenceAsInterface(asyncLifetimeType);

            generationContext.TestClass.BaseTypes.Add(asyncLifetimeType);

            // async Task IAsyncLifetime.DisposeAsync() { <fixtureTearDownMethod>(); }

            CodeMemberMethod disposeMethod = new CodeMemberMethod();
            
            disposeMethod.PrivateImplementationType = new CodeTypeReference(IASYNCLIFETIME_INTERFACE);
            disposeMethod.Name = "DisposeAsync";
            disposeMethod.ReturnType = new CodeTypeReference(typeof(Task));
            _currentFixtureDataTypeDeclaration.Members.Add(disposeMethod);
            
            CodeDomHelper.MarkCodeMemberMethodAsAsync(disposeMethod);

            var expression = new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(new CodeTypeReference(generationContext.TestClass.Name)),
                generationContext.TestClassCleanupMethod.Name);

            CodeDomHelper.MarkCodeMethodInvokeExpressionAsAwait(expression);

            disposeMethod.Statements.Add(expression);
        }

        public void SetTestMethod(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string friendlyTestName)
        {
            testMethod.ReturnType = new CodeTypeReference(typeof(Task));

            CodeDomHelper.MarkCodeMemberMethodAsAsync(testMethod);

            CodeDomHelper.AddAttribute(testMethod, FACT_ATTRIBUTE, new CodeAttributeArgument("DisplayName",new CodePrimitiveExpression(friendlyTestName)));

            SetProperty(testMethod, FEATURE_TITLE_PROPERTY_NAME, generationContext.Feature.Name);
            SetDescription(testMethod, friendlyTestName);
        }

        public virtual void SetRowTest(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle)
        {
            CodeDomHelper.AddAttribute(testMethod, THEORY_ATTRIBUTE);

            SetProperty(testMethod, FEATURE_TITLE_PROPERTY_NAME, generationContext.Feature.Name);
            SetDescription(testMethod, scenarioTitle);
        }

        public virtual void SetRow(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> arguments, IEnumerable<string> tags, bool isIgnored)
        {
            //TODO: better handle "ignored"
            if (isIgnored)
                return;

            var args = arguments.Select(
              arg => new CodeAttributeArgument(new CodePrimitiveExpression(arg))).ToList();

            args.Add(
                new CodeAttributeArgument(
                    new CodeArrayCreateExpression(typeof(string[]), tags.Select(t => new CodePrimitiveExpression(t)).ToArray())));

            CodeDomHelper.AddAttribute(testMethod, INLINEDATA_ATTRIBUTE, args.ToArray());
        }

        public virtual void SetTestMethodCategories(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, IEnumerable<string> scenarioCategories)
        {
            foreach (string str in scenarioCategories)
                SetProperty((CodeTypeMember)testMethod, "Category", str);
        }

        public void SetTestInitializeMethod(TestClassGenerationContext generationContext)
        {
            // xUnit uses a parameterless constructor

            // public <_currentTestTypeDeclaration>() { <memberMethod>(); }

            CodeConstructor ctorMethod = new CodeConstructor();
            ctorMethod.Attributes = MemberAttributes.Public;
            generationContext.TestClass.Members.Add(ctorMethod);

            SetTestInitializeMethod(generationContext, ctorMethod);

            // Task IAsyncLifetime.InitializeAsync() { <memberMethod>(); }

            CodeMemberMethod initializeMethod = new CodeMemberMethod();

            initializeMethod.PrivateImplementationType = new CodeTypeReference(IASYNCLIFETIME_INTERFACE);
            initializeMethod.Name = "InitializeAsync";
            initializeMethod.ReturnType = new CodeTypeReference(typeof(Task));

            CodeDomHelper.MarkCodeMemberMethodAsAsync(initializeMethod);

            generationContext.TestClass.Members.Add(initializeMethod);
        }

        public void SetTestCleanupMethod(TestClassGenerationContext generationContext)
        {
            // xUnit supports test tear down through the IAsyncLifetime interface

            // Task IAsyncLifetime.DisposeAsync() { <memberMethod>(); }

            CodeMemberMethod disposeMethod = new CodeMemberMethod();
            
            disposeMethod.PrivateImplementationType = new CodeTypeReference(IASYNCLIFETIME_INTERFACE);
            disposeMethod.Name = "DisposeAsync";
            disposeMethod.ReturnType = new CodeTypeReference(typeof(Task));
            generationContext.TestClass.Members.Add(disposeMethod);

            CodeDomHelper.MarkCodeMemberMethodAsAsync(disposeMethod);
            
            var expression = new CodeMethodInvokeExpression(
                new CodeThisReferenceExpression(),
                generationContext.TestCleanupMethod.Name);

            CodeDomHelper.MarkCodeMethodInvokeExpressionAsAwait(expression);

            disposeMethod.Statements.Add(expression);
        }

        public void SetTestClassIgnore(TestClassGenerationContext generationContext)
        {
            // The individual tests have to get Skip argument in their attributes
            // xUnit does not provide a way to Skip a set of tests - https://xunit.github.io/docs/comparisons.html#attributes
            // This is handled in FinalizeTestClass
        }

        public virtual void SetTestMethodIgnore(TestClassGenerationContext generationContext, CodeMemberMethod testMethod)
        {
            var factAttr = testMethod.CustomAttributes.OfType<CodeAttributeDeclaration>()
                .FirstOrDefault(codeAttributeDeclaration => codeAttributeDeclaration.Name == FACT_ATTRIBUTE);

            if (factAttr != null)
            {
                // set [FactAttribute(Skip="reason")]
                factAttr.Arguments.Add
                    (
                        new CodeAttributeArgument(FACT_ATTRIBUTE_SKIP_PROPERTY_NAME, new CodePrimitiveExpression(SKIP_REASON))
                    );
            }

            var theoryAttr = testMethod.CustomAttributes.OfType<CodeAttributeDeclaration>()
                .FirstOrDefault(codeAttributeDeclaration => codeAttributeDeclaration.Name == THEORY_ATTRIBUTE);

            if (theoryAttr != null)
            {
                // set [TheoryAttribute(Skip="reason")]
                theoryAttr.Arguments.Add
                    (
                        new CodeAttributeArgument(THEORY_ATTRIBUTE_SKIP_PROPERTY_NAME, new CodePrimitiveExpression(SKIP_REASON))
                    );
            }
        }

        protected virtual void SetTestInitializeMethod(TestClassGenerationContext generationContext, CodeMemberMethod method) {
          method.Statements.Add(
              new CodeMethodInvokeExpression(
                  new CodeThisReferenceExpression(),
                  generationContext.TestInitializeMethod.Name));
        }

        protected void SetProperty(CodeTypeMember codeTypeMember, string name, string value)
        {
            CodeDomHelper.AddAttribute(codeTypeMember, TRAIT_ATTRIBUTE, name, value);
        }

        protected void SetDescription(CodeTypeMember codeTypeMember, string description)
        {
            // xUnit doesn't have a DescriptionAttribute so using a TraitAttribute instead
            SetProperty(codeTypeMember, DESCRIPTION_PROPERTY_NAME, description);
        }

        protected virtual CodeTypeReference CreateFixtureInterface(TestClassGenerationContext generationContext, CodeTypeReference fixtureDataType)
        {
            var useFixtureType = new CodeTypeReference(IUSEFIXTURE_INTERFACE, fixtureDataType);
            // public void SetFixture(T) { } // explicit interface implementation for generic interfaces does not work with codedom

            CodeMemberMethod setFixtureMethod = new CodeMemberMethod();
            setFixtureMethod.Attributes = MemberAttributes.Public;
            setFixtureMethod.Name = "SetFixture";
            setFixtureMethod.Parameters.Add(new CodeParameterDeclarationExpression(fixtureDataType, "fixtureData"));
            setFixtureMethod.ImplementationTypes.Add(useFixtureType);
            generationContext.TestClass.Members.Add(setFixtureMethod);

            return useFixtureType;
        }

        public virtual void FinalizeTestClass(TestClassGenerationContext generationContext)
        {
            IgnoreFeature(generationContext);
        }

        protected virtual void IgnoreFeature(TestClassGenerationContext generationContext)
        {
            var featureHasIgnoreTag = generationContext.Feature.Tags
               .Any(x => string.Equals(x.Name, IGNORE_TAG, StringComparison.InvariantCultureIgnoreCase));

            if (featureHasIgnoreTag)
            {
                foreach (CodeTypeMember member in generationContext.TestClass.Members)
                {
                    var method = member as CodeMemberMethod;
                    if (method != null && !IsTestMethodAlreadyIgnored(method))
                    {
                        SetTestMethodIgnore(generationContext, method);
                    }
                }
            }
        }

        protected virtual bool IsTestMethodAlreadyIgnored(CodeMemberMethod testMethod)
        {
            return IsTestMethodAlreadyIgnored(testMethod, FACT_ATTRIBUTE, THEORY_ATTRIBUTE);
        }

        protected static bool IsTestMethodAlreadyIgnored(CodeMemberMethod testMethod, string factAttributeName, string theoryAttributeName)
        {
            var factAttr = testMethod.CustomAttributes.OfType<CodeAttributeDeclaration>()
                .FirstOrDefault(codeAttributeDeclaration => codeAttributeDeclaration.Name == factAttributeName);

            var hasIgnoredFact = factAttr?.Arguments.OfType<CodeAttributeArgument>()
                .Any(x =>
                    string.Equals(x.Name, FACT_ATTRIBUTE_SKIP_PROPERTY_NAME, StringComparison.InvariantCultureIgnoreCase));

            var theoryAttr = testMethod.CustomAttributes.OfType<CodeAttributeDeclaration>()
               .FirstOrDefault(codeAttributeDeclaration => codeAttributeDeclaration.Name == theoryAttributeName);

            var hasIgnoredTheory = theoryAttr?.Arguments.OfType<CodeAttributeArgument>()
                .Any(x =>
                    string.Equals(x.Name, THEORY_ATTRIBUTE_SKIP_PROPERTY_NAME, StringComparison.InvariantCultureIgnoreCase));

            var result = hasIgnoredFact.GetValueOrDefault() || hasIgnoredTheory.GetValueOrDefault();

            return result;
        }

        public void SetTestMethodAsRow(TestClassGenerationContext generationContext, CodeMemberMethod testMethod, string scenarioTitle, string exampleSetName, string variantName, IEnumerable<KeyValuePair<string, string>> arguments)
        {
            // doing nothing since we support RowTest
        }

        public void MarkCodeMemberMethodAsAsync(CodeMemberMethod testMethod)
        {
            CodeDomHelper.MarkCodeMemberMethodAsAsync(testMethod);
        }

        public void MarkCodeMethodInvokeExpressionAsAwait(CodeMethodInvokeExpression expression)
        {
            CodeDomHelper.MarkCodeMethodInvokeExpressionAsAwait(expression);
        }
    }
}
