# Il2CppInspectorPro 2025

Il2CppInspectorPro helps you to reverse engineer IL2CPP applications, providing the most complete analysis currently available.

This is a continuation of [Il2CppInspector, by djkaty](https://github.com/djkaty/Il2CppInspector) which has suspended development, and contains some new features and support for new metadata versions.

![Il2CppInspector GUI](docs/GUI_Preview.png)

### Redux/Pro only features

* Support for metadata version 29/29.1/31/35/38, with full reconstruction of custom attributes
* Proper extraction of static array initializer contents with their correct length
* Proper support for v27.2+ Il2CppType
* Fixed support for v24.5
* Export of static metadata fields and IDA script support for importing them
* Support for [ThreadStatic] static fields
* Better heuristic for detecting metadata usages
* Performance improvements

### License

This software is licensed under AGPLv3.
