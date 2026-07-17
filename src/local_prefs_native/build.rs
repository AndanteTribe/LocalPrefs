use std::{fs, path::PathBuf};

fn main() {
    let output_path = PathBuf::from(env!("CARGO_MANIFEST_DIR")).join(
        "../LocalPrefs.Unity/Packages/jp.andantetribe.localprefs/Runtime/LocalPrefsNative.g.cs",
    );

    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .csharp_dll_name("local_prefs_native")
        .csharp_dll_name_if("UNITY_WEBGL && !UNITY_EDITOR", "__Internal")
        .csharp_namespace("AndanteTribe.IO.Unity")
        .csharp_class_name("LocalPrefsNative")
        .csharp_class_accessibility("internal")
        .csharp_use_function_pointer(false)
        .generate_csharp_file(&output_path)
        .unwrap();

    let bindings = fs::read_to_string(&output_path).unwrap();
    let bindings = bindings.replace("__DllName", "DllName").replace(
        "        const string DllName",
        "        private const string DllName",
    );
    fs::write(&output_path, bindings).unwrap();

    println!("cargo:rerun-if-changed=src/lib.rs");
}
