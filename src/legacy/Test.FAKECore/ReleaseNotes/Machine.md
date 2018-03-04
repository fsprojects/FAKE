# Changelog
## 1.8.0
* When the subject's constructor throws an exception, it is now bubbled up and shown in the failure message.
* Fixed an exception in the Rhino Mocks adapter when faking a delegate (thanks to [Alexis Atkinson](https://github.com/alexisatkinson)).
* Updated to FakeItEasy 1.14.0
* Updated to Machine.Specifications 0.5.16
* Updated to Moq 4.1.1309.1617

## 1.7.0
* Added support for setting up values for out and ref parameters to FakeItEasy and NSubsitute adapters.
* Updated to Machine.Specifications 0.5.14
* Updated to FakeItEasy 1.13.1
* Updated to NSubsitute 1.6.1.0

## 1.6.0
* Fixed `WithFakes` to work correctly under Gallio test runner (thanks to [Alexander Abramov](https://github.com/alexanderabramov))
* Updated to FakeItEasy 1.12.0

## 1.5.0
* `The()` and `Configure()` are now available on `WithFakes`.
* Updated to FakeItEasy 1.11.0
* Updated to NSubsitute 1.6.0.0

## 1.4.0
* Updated to FakeItEasy 1.10.0
* `WithSubject` and `WithFakes` are marked as used implicitly for ReSharper now.

## 1.3.0
* Added support for `BehaviorConfig`s to `WithFakes` (thanks to [Robert Anderson](https://github.com/shamp00))

## 1.2.0
* Updated to Machine.Specifications 0.5.12
* Updated to FakeItEasy 1.9.1
* Better error handling when `WithFakes` or `WithSubject` are called before being initialized

## 1.1.0
* Updated to Machine.Specifications 0.5.11
* Updated to FakeItEasy 1.8.0
* Updated to NSubstitute 1.5.0.0

## 1.0.4
* `Param.Is()` now works correctly with all sorts of expressions, not only constant values.

## 1.0.3
* Constructors with pointer parameters are now filtered when instantiating concrete types. This fixes a bug that prevented a string constructor parameter to be automatically resolved to the empty string.

## 1.0.2
* Updated to Machine.Specifications 0.5.10 and FakeItEasy 1.7.4626.65

## 1.0.1
* Added documentation XML to NuGet packages, so that class and member documentation is available in Visual Studio.

## 1.0.0
* **Breaking change**: dependency resolution has changed internally. The basic functionality should be the same, but there are some new features:
 * Types with value type constructor parameters can be instantiated.
 * `IEnumerable<T>` and `Array` dependencies can be configured directly. For example
    `Configure<IEnumerable<int>>(new List<int> { 1, 2, 5 });`
 * `Lazy<T>` works like `Func<T>` as a dependency.
 * When a type has multiple greediest constructors, the one with the most configured parameters is chosen.

 If this breaks existing tests, please revert to a previous version temporarily and file an issue on [github](https://github.com/machine/machine.fakes/issues).
* **Breaking change**: `IMethodCallOccurance` has been renamed to `IMethodCallOccurrence` and all inheriting classes accordingly.

## 0.6.0
* Fakes created with the Rhino Mocks adapter will now automatically track their property values. (Thanks to [Albert Weinert](https://github.com/DerAlbertCom))
* Updated to Machine.Specifications 0.5.9, NSubstitute 1.4.3 and FakeItEasy 1.7.4582.63.

## 0.5.1
* New icon

## 0.5.0
* Fakes created with the Moq adapter will now automatically track their property values. (Thanks to [Jason Duffett](https://github.com/laazyj))
* Updated to NSubstitute 1.4.2.0.

## 0.4.0
* Machine.Fakes is considered forward compatible to future versions of Machine.Specifications from now on.
* Updated to require at least Machine.Specifications 0.5.7
* Updated to require at least FakeItEasy 1.7.4507.61 and NSubstitute 1.4.0.0.