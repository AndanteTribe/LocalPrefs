(() => {
    "use strict";

    if (globalThis.localPrefsNative) {
        return;
    }

    const databaseName = "/localprefs";
    const storeName = "MAIN";

    function malloc(size) {
        const allocate = Module._malloc || (Module.asm && Module.asm.malloc);
        if (!allocate) {
            throw new Error("Emscripten malloc is unavailable.");
        }

        return allocate(Math.max(size, 1));
    }

    function free(pointer) {
        const release = Module._free || (Module.asm && Module.asm.free);
        if (!release) {
            throw new Error("Emscripten free is unavailable.");
        }

        release(pointer);
    }

    function callWasm(callback, signature, ...args) {
        if (typeof getWasmTableEntry !== "undefined") {
            getWasmTableEntry(callback)(...args);
            return;
        }

        const dynCall = Module[`dynCall_${signature}`];
        if (typeof dynCall === "function") {
            dynCall(callback, ...args);
            return;
        }

        throw new Error("No Emscripten dynamic-call implementation is available.");
    }

    function errorMessage(error) {
        if (error && typeof error.message === "string") {
            return error.message;
        }

        return String(error || "Browser storage operation failed.");
    }

    function reportError(state, callback, error) {
        const message = errorMessage(error);
        const size = lengthBytesUTF8(message) + 1;
        const pointer = malloc(size);
        try {
            stringToUTF8(message, pointer, size);
            callWasm(callback, "vii", state, pointer);
        } finally {
            free(pointer);
        }
    }

    function openDatabase(onSuccess, onError) {
        if (typeof indexedDB === "undefined") {
            onError(new Error("IndexedDB is not supported in this environment."));
            return;
        }

        let request;
        try {
            request = indexedDB.open(databaseName, 1);
        } catch (error) {
            onError(error);
            return;
        }

        request.onupgradeneeded = event => {
            const database = event.target.result;
            if (!database.objectStoreNames.contains(storeName)) {
                database.createObjectStore(storeName);
            }
        };
        request.onsuccess = event => onSuccess(event.target.result);
        request.onerror = () => onError(request.error);
        request.onblocked = () => onError(new Error("Opening IndexedDB was blocked."));
    }

    globalThis.localPrefsNative = Object.freeze({
        saveToLocalStorage(keyPointer, valuePointer) {
            if (typeof localStorage === "undefined") {
                throw new Error("Local Storage is not supported in this environment.");
            }

            localStorage.setItem(UTF8ToString(keyPointer), UTF8ToString(valuePointer));
        },

        deleteFromLocalStorage(keyPointer) {
            if (typeof localStorage === "undefined") {
                throw new Error("Local Storage is not supported in this environment.");
            }

            localStorage.removeItem(UTF8ToString(keyPointer));
        },

        loadFromLocalStorage(keyPointer) {
            if (typeof localStorage === "undefined") {
                throw new Error("Local Storage is not supported in this environment.");
            }

            const value = localStorage.getItem(UTF8ToString(keyPointer));
            if (value === null) {
                return 0;
            }

            const size = lengthBytesUTF8(value) + 1;
            const pointer = malloc(size);
            stringToUTF8(value, pointer, size);
            return pointer;
        },

        saveToIndexedDB(state, keyPointer, dataPointer, dataSize, success, error) {
            const key = UTF8ToString(keyPointer);
            const data = Module.HEAPU8.slice(dataPointer, dataPointer + dataSize);
            let completed = false;
            let database = null;

            const fail = reason => {
                if (completed) {
                    return;
                }

                completed = true;
                try {
                    reportError(state, error, reason);
                } finally {
                    if (database) {
                        database.close();
                    }
                }
            };

            openDatabase(openedDatabase => {
                database = openedDatabase;
                let transaction;
                try {
                    transaction = database.transaction(storeName, "readwrite");
                    transaction.objectStore(storeName).put(data, key);
                } catch (reason) {
                    fail(reason);
                    return;
                }

                transaction.oncomplete = () => {
                    if (completed) {
                        return;
                    }

                    completed = true;
                    try {
                        callWasm(success, "vi", state);
                    } finally {
                        database.close();
                    }
                };
                transaction.onerror = () => fail(transaction.error);
                transaction.onabort = () => fail(transaction.error || new Error("IndexedDB transaction was aborted."));
            }, fail);
        },

        deleteFromIndexedDB(state, keyPointer, success, error) {
            const key = UTF8ToString(keyPointer);
            let completed = false;
            let database = null;

            const fail = reason => {
                if (completed) {
                    return;
                }

                completed = true;
                try {
                    reportError(state, error, reason);
                } finally {
                    if (database) {
                        database.close();
                    }
                }
            };

            openDatabase(openedDatabase => {
                database = openedDatabase;
                let transaction;
                try {
                    transaction = database.transaction(storeName, "readwrite");
                    transaction.objectStore(storeName).delete(key);
                } catch (reason) {
                    fail(reason);
                    return;
                }

                transaction.oncomplete = () => {
                    if (completed) {
                        return;
                    }

                    completed = true;
                    try {
                        callWasm(success, "vi", state);
                    } finally {
                        database.close();
                    }
                };
                transaction.onerror = () => fail(transaction.error);
                transaction.onabort = () => fail(transaction.error || new Error("IndexedDB transaction was aborted."));
            }, fail);
        },

        loadFromIndexedDB(state, keyPointer, success, error) {
            const key = UTF8ToString(keyPointer);
            let completed = false;
            let database = null;

            const fail = reason => {
                if (completed) {
                    return;
                }

                completed = true;
                try {
                    reportError(state, error, reason);
                } finally {
                    if (database) {
                        database.close();
                    }
                }
            };

            openDatabase(openedDatabase => {
                database = openedDatabase;
                let request;
                try {
                    const transaction = database.transaction(storeName, "readonly");
                    request = transaction.objectStore(storeName).get(key);
                    transaction.onerror = () => fail(transaction.error);
                    transaction.onabort = () => fail(transaction.error || new Error("IndexedDB transaction was aborted."));
                } catch (reason) {
                    fail(reason);
                    return;
                }

                request.onsuccess = () => {
                    if (completed) {
                        return;
                    }

                    const result = request.result;
                    if (typeof result === "undefined") {
                        fail(new Error("Key not found"));
                        return;
                    }

                    const data = result instanceof Uint8Array ? result : new Uint8Array(result);
                    const pointer = malloc(data.byteLength);
                    Module.HEAPU8.set(data, pointer);
                    completed = true;
                    try {
                        callWasm(success, "viii", state, pointer, data.byteLength);
                    } finally {
                        free(pointer);
                        database.close();
                    }
                };
                request.onerror = () => fail(request.error);
            }, fail);
        },
    });
})();
