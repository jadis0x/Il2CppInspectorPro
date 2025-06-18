# Il2CppInspectorPro 2025.2
![Il2CppInspector GUI](docs/il2cppPro_preview.png)

<b>Il2CppInspectorPro</b> helps you to reverse engineer IL2CPP applications, providing the most complete analysis currently available.

This is a continuation of [Il2CppInspector, by djkaty](https://github.com/djkaty/Il2CppInspector) which has suspended development, and contains some new features and support for new metadata versions.


## Pro Features

### Advanced C++ Scaffold System

In this version, the scaffold project is no longer just a basic skeleton, it's evolved into a fully customizable framework:

<ul>
    <li>
        <b>Project Name Customization:</b> You can now define your own project name when generating the scaffold.
   </li>
</ul>

<b>Modular Library Support:</b> Easily include the components you need with a single click:

<ul>
    <li><b>ImGui:</b> Lightweight and powerful GUI support.</li>
    <li><b>Detours:</b> API hooking made simple and straightforward.</li>
    <li><b>DLL proxy for Windows versioning API: </b> Custom version.dll proxy for injecting code into target processes without an injector. </li>
    <li><b>Jadis0x's IL2CPP Resolver: </b> Simplifies calling Unity functions using IL2CPP APIs. </li>
</ul>

### Version support

Unity version | IL2CPP version | Support
--- | --- | ---
4.6.1+ | First release | Unsupported
5.2.x | 15 | Unsupported
5.3.0-5.3.1 | 16 | Working
5.3.2 | 19 | Working
5.3.3-5.3.4 | 20 | Working
5.3.5-5.4.6 | 21 | Working
5.5.0-5.5.6 | 22 | Working
5.6.0-5.6.7 | 23 | Working
2017.1.0-2018.2.21 | 24 | Working
2018.3.0-2018.4.x | 24.1 | Working
2019.1.0-2019.3.6 | 24.2 | Working
2019.3.7-2019.4.14 | 24.3 | Working
2019.4.15-2019.4.20 | 24.4 | Working
2019.4.21-2019.4.x | 24.5 | Working
2020.1.0-2020.1.10 | 24.3 | Working
2020.1.11-2020.1.17 | 24.4 | Working
2020.2.0-2020.2.3 | 27 | Working
2020.2.4-2020.3.x | 27.1 | Working
2021.1.0-2021.1.x | 27.2 | Working
2021.2.0-2021.2.x | 29 | Working
2021.3.0+ | 29.1 | Working
2022.3.33+ | 31(.1) | Working

Please refer to the companion repository https://github.com/nneonneo/Il2CppVersions if you would like to track the changes between each IL2CPP release version.

#### Il2CppInspectorPro guides

[Il2cpp Reverse Engineering Guide by Jadis0x](https://github.com/jadis0x/il2cpp-reverse-engineering-guide)

For more detailed information about Il2CppInspectorPro, [visit here](https://github.com/djkaty/Il2CppInspector).

### License

This software is licensed under AGPLv3.
