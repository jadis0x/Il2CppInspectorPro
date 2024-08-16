#pragma once

struct Il2CppImage;
struct Il2CppDomain;
struct Il2CppAssembly;
struct Il2CppClass;
struct Il2CppType;
struct Il2CppObject;

class Il2cppBridge {
public:
	Il2cppBridge(const char* imageName);
	Il2cppBridge& OpenAssembly(const char* imageName);
	Il2cppBridge& find_class(const char* classNamespace, const char* className);
	Il2CppObject* get_class();
public:
	const Il2CppImage* GetCurrentImage();
private:
	Il2CppDomain* domain;
private:
	const Il2CppAssembly* currentAssembly;
	Il2CppObject* obj;
};


