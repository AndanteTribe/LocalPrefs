use core::ffi::{c_char, c_int, c_void};
use std::ffi::{CStr, CString};
use std::sync::LazyLock;

static LIBRARY: LazyLock<Library> = LazyLock::new(Library::new);

struct Library;

unsafe extern "C" {
    fn emscripten_run_script(script: *const c_char);
    fn emscripten_run_script_int(script: *const c_char) -> c_int;
    fn free(pointer: *mut c_void);
}

impl Library {
    fn new() -> Self {
        run_script(
            CString::new(include_str!("js/library.js"))
                .unwrap()
                .as_c_str(),
        );
        Self
    }

    fn run(&self, script: String) {
        run_script(CString::new(script).unwrap().as_c_str());
    }

    fn run_int(&self, script: String) -> c_int {
        run_script_int(CString::new(script).unwrap().as_c_str())
    }
}

fn run_script(script: &CStr) {
    unsafe { emscripten_run_script(script.as_ptr()) };
}

fn run_script_int(script: &CStr) -> c_int {
    unsafe { emscripten_run_script_int(script.as_ptr()) }
}

#[unsafe(no_mangle)]
/// Saves a UTF-8 value to browser local storage.
///
/// # Safety
///
/// `key` and `value` must point to valid null-terminated UTF-8 strings for the duration of the call.
pub unsafe extern "C" fn local_prefs_save_to_local_storage(
    key: *const c_char,
    value: *const c_char,
) {
    LIBRARY.run(format!(
        "globalThis.localPrefsNative.saveToLocalStorage({}, {});",
        key as usize, value as usize
    ));
}

#[unsafe(no_mangle)]
/// Deletes a value from browser local storage.
///
/// # Safety
///
/// `key` must point to a valid null-terminated UTF-8 string for the duration of the call.
pub unsafe extern "C" fn local_prefs_delete_from_local_storage(key: *const c_char) {
    LIBRARY.run(format!(
        "globalThis.localPrefsNative.deleteFromLocalStorage({});",
        key as usize
    ));
}

#[unsafe(no_mangle)]
/// Loads a UTF-8 value from browser local storage.
///
/// # Safety
///
/// `key` must point to a valid null-terminated UTF-8 string for the duration of the call. The
/// returned pointer must be released exactly once with [`local_prefs_free`].
pub unsafe extern "C" fn local_prefs_load_from_local_storage(key: *const c_char) -> *mut c_char {
    LIBRARY.run_int(format!(
        "globalThis.localPrefsNative.loadFromLocalStorage({});",
        key as usize
    )) as usize as *mut c_char
}

#[unsafe(no_mangle)]
/// Releases memory allocated by the embedded browser bridge.
///
/// # Safety
///
/// `pointer` must be null or a pointer returned by `local_prefs_load_from_local_storage` that has
/// not already been released.
pub unsafe extern "C" fn local_prefs_free(pointer: *mut c_void) {
    if !pointer.is_null() {
        unsafe { free(pointer) };
    }
}

#[unsafe(no_mangle)]
/// Starts an IndexedDB save operation.
///
/// # Safety
///
/// All pointers must remain valid until the embedded browser bridge has copied their values.
/// `success` and `error` must be valid C callback pointers with the signatures expected by the
/// bridge.
pub unsafe extern "C" fn local_prefs_save_to_indexed_db(
    state: *mut c_void,
    key: *const c_char,
    data: *const u8,
    data_size: c_int,
    success: *const c_void,
    error: *const c_void,
) {
    LIBRARY.run(format!(
        "globalThis.localPrefsNative.saveToIndexedDB({}, {}, {}, {}, {}, {});",
        state as usize, key as usize, data as usize, data_size, success as usize, error as usize
    ));
}

#[unsafe(no_mangle)]
/// Starts an IndexedDB delete operation.
///
/// # Safety
///
/// `key` must remain valid until the embedded browser bridge has copied it. `success` and `error`
/// must be valid C callback pointers with the signatures expected by the bridge.
pub unsafe extern "C" fn local_prefs_delete_from_indexed_db(
    state: *mut c_void,
    key: *const c_char,
    success: *const c_void,
    error: *const c_void,
) {
    LIBRARY.run(format!(
        "globalThis.localPrefsNative.deleteFromIndexedDB({}, {}, {}, {});",
        state as usize, key as usize, success as usize, error as usize
    ));
}

#[unsafe(no_mangle)]
/// Starts an IndexedDB load operation.
///
/// # Safety
///
/// `key` must remain valid until the embedded browser bridge has copied it. `success` and `error`
/// must be valid C callback pointers with the signatures expected by the bridge.
pub unsafe extern "C" fn local_prefs_load_from_indexed_db(
    state: *mut c_void,
    key: *const c_char,
    success: *const c_void,
    error: *const c_void,
) {
    LIBRARY.run(format!(
        "globalThis.localPrefsNative.loadFromIndexedDB({}, {}, {}, {});",
        state as usize, key as usize, success as usize, error as usize
    ));
}
