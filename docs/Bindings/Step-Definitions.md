# Step Definitions

The bindings (step definitions, [hooks](Hooks.md), etc.) provide the connection between your feature files and application interfaces (see [Bindings](Bindings.md)). For better reusability, the step definitions can include parameters. This means that it is not necessary to define a new step definition for each step that just differs slightly. For example, the steps `When I perform a simple search on 'Domain'` and `When I perform a simple search on 'Communication'` can be automated with a single step definition, with 'Domain' and 'Communication' as parameters.  

The following example shows a simple step definition that matches to the step `When I perform a simple search on 'Domain'`:

```c#
[When(@"I perform a simple search on '(.*)'")]
public void WhenIPerformASimpleSearchOn(string searchTerm)
{
    var controller = new CatalogController();
    actionResult = controller.Search(searchTerm);
}
```

Here the method is annotated with the `[When]` attribute, and includes the regular expression used to match the step's text. This regular expression uses (`(.*)`) to define parameters for the method.

Although the step definitions are usually invoked by the SpecFlow runtime, you can also call steps from other step definitions. Check the [Calling Steps from Step Definitions](Calling-Steps-from-Step-Definitions.md) page for details.

## Supported Step Definition Attributes

* `[Given(regex)]` or `[Given]` - `TechTalk.SpecFlow.GivenAttribute`
* `[When(regex)]` or `[When]` - `TechTalk.SpecFlow.WhenAttribute`
* `[Then(regex)]` or `[Then]` - `TechTalk.SpecFlow.ThenAttribute`
* `[StepDefinition(regex)]` or `[StepDefinition]` - `TechTalk.SpecFlow.StepDefinitionAttribute`, matches for given, when or then attributes

You can annotate a single method with multiple attributes in order to support different phrasings in the feature file for the same automation logic.

```c#
[When(@"I perform a simple search on '(.*)'")]
[When(@"I search for '(.*)'")]
public void WhenIPerformASimpleSearchOn(string searchTerm)
{
  ...
}
```

## Rules for the Step Definition Method

* Must be in a public class, marked with the `[Binding]` attribute.
* Must be a public method.
* Can be either a static or an instance method. If it is an instance method, the containing class will be instantiated once for every scenario.
* Cannot have `out` or `ref` parameters.
* Cannot have a return type. 

## Step Matching Rules

* The rules depend on the step definition style you use. Check the [step definition styles](#step-definition-styles) page for the exact rules.

## Parameter Matching Rules

* Step definitions can specify parameters. These will match to the parameters of the step definition method.
* The method parameter type can be `string` or other .NET type. In the later case [configurable conversion](Step-Argument-Conversions.md) is applied.
* With regular expressions
  * The match groups (`(…)`) of the regular expression define the arguments for the method based on the order (the match result of the first group becomes the first argument, etc.).
  * You can use non-capturing groups `(?:regex)` in order to use groups without a method argument.
* With method name matching
  * You can refer to the method parameters with either the parameter name (ALL-CAPS) or the parameter index (zero-based) with the `P` prefix, e.g. `P0`.

## Table or Multi-line Text Arguments

If the step definition method should match for steps having [table or multi-line text arguments](../Gherkin/Using-Gherkin-Language-in-SpecFlow.md), additional `Table` and/or `string` parameters have to be defined in the method signature to be able to receive these arguments. If both table and multi-line text argument are used for the step, the multi-line text argument is provided first.

``` gherkin
Given the following books
  |Author        |Title                          |
  |Martin Fowler |Analysis Patterns              |
  |Gojko Adzic   |Bridging the Communication Gap |
```

``` csharp
[Given(@"the following books")]
public void GivenTheFollowingBooks(Table table)
{
  ...
}
```

## Step Definition Styles

Step definitions provide the connection between the free-text specification steps and the application interfaces. Step definitions are .NET methods that match to certain scenario steps.

The classic way of providing the match rules is to annotate the method with regular expressions. From SpecFlow 1.9 however, you can also create the step definitions without regex.

This page contains information about the **different styles**, the **step definition skeleton generation** and about the **custom templates**. For general rules of step definitions, please check the Step Definitions page.

### Regular expressions in attributes

This is the classic way of specifying the step definitions. The step definition method has to be annotated with one or more step definition attributes with regular expressions.

```c#
[Given(@"I have entered (.*) into the calculator")]
public void GivenIHaveEnteredNumberIntoTheCalculator(int number)
{
  ...
}
```

Regular expression matching rules:

* Regular expressions are always matched to the entire step, even if you do not use the `^` and `$` markers.
* The capturing groups (`(…)`) in the regular expression define the arguments for the method in order (the result of the first group becomes the first argument etc.).
* You can use non-capturing groups `(?:regex)` in order to use groups without a method argument.

### Method name - underscores

Most of the step definitions can also be specified without regular expression: the method should be named with underscored naming style (or pascal-case, see below) and should be annotated with an empty `[Given]`, `[When]`, `[Then]` or `[StepDefinition]` attribute. You can refer to the method parameters with either the parameter name (ALL-CAPS) or the parameter index (zero-based) with the `P` prefix, e.g. `P0`.

```c#
[Given]
public void Given_I_have_entered_NUMBER_into_the_calculator(int number)
{
  ...
}
```

Matching rules:

* The match is case-insensitive.
* Underscore character is matched to one or more non-word character (eg. whitespace, punctuation): `\W+`.
* If the step contains accented characters, the method name should also contain the accented characters (no substitution). 
* The step keyword (e.g. `Given`) can be omitted: `[Given] public void I_have_entered_NUMBER_...`.
* The step keyword can be specified in the local Gherkin language, or English. The default language can be specified in the [app config](../Configuration/Configuration.md) as the feature language or binding culture. The following step definition is threfore a valid "Given" step with German language settings: `[When] public void Wenn_ich_addieren_drücke()`

More detailed examples can be found in our specs: [StepDefinitionsWithoutRegex.feature](https://github.com/SpecFlowOSS/SpecFlow/blob/master/Tests/TechTalk.SpecFlow.Specs/Features/Execution/StepDefinitionsWithoutRegex.feature).

### Method name - Pascal-case

Similarly to the underscored naming style, you can also specify the step definitions with Pascal-case method names. 

```c#
[Given]
public void GivenIHaveEnteredNUMBERIntoTheCalculator(int number)
{
  ...
}
```

Matching rules:

* All rules of "Method name - underscores" style applied.
* Any potential word boundary (e.g. number-letter, uppercase-lowercase, uppercase-uppercase) is matching to zero or more non-word character (eg. whitespace, punctuation): `\W*`.
* You can mix this style with underscores. For example, the parameter placeholders can be highlighted this way: `GivenIHaveEntered_P0_IntoTheCalculator(int number)`

### Method name - regex (F# only)

F# allows providing any characters in method names, so you can make the regular expression as the method name, if you use [F# bindings](FSharp-Support.md).

```F#
let [<Given>] ``I have entered (.*) into the calculator``(number:int) = 
    Calculator.Push(number)
```

## See also

* [Step Argument Conversions](Step-Argument-Conversions.md)
* [Scoped Bindings](Scoped-Step-Definitions.md)
