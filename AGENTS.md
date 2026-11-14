# Valheim development

- Validate game behavior, member visibility, and Harmony hook signatures against the installed Valheim assemblies before implementing changes. The runtime assembly is `G:\SteamLibrary\steamapps\common\Valheim\Valheim_Data\Managed\assembly_valheim.dll`.
- Publicized build references do not change runtime member visibility. Use public APIs or Harmony access mechanisms for private members.
- Consult the Valheim Modding Wiki when applicable, including its animation replacement guide, and Jotunn documentation for Jotunn APIs.
- The local Jotunn reference is the compiled DLL at `C:\Users\Jonathan\Documents\valheim_mods\Jotunn-2.30.0\plugins\Jotunn.dll`. Inspect its compiled code when validating Jotunn behavior.
