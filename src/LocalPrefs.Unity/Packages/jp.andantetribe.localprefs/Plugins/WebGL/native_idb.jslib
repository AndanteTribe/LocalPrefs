const plugin = {

    $Config: { DB_NAME: "/localprefs", STORE_NAME: "MAIN", DEBUG_STORE_NAME: "DEBUG" },

    SaveToIndexedDB: function (statePtr, keyStr, dataPtr, dataSize, success, error) {
        const key = UTF8ToString(keyStr);
        const data = HEAPU8.slice(dataPtr, dataPtr + dataSize);
        let completed = false;

        function closeDatabase(db) {
            if (db) {
                db.close();
            }
        }

        function fail(reason, db) {
            if (completed) {
                closeDatabase(db);
                return;
            }

            completed = true;
            closeDatabase(db);
            const cause = reason && reason.target ? reason.target.error : reason;
            const message = cause && cause.message ? cause.message : "IndexedDB write failed.";
            const buffer = stringToNewUTF8(message);
            try {
                {{{ makeDynCall('vii', 'error') }}}(statePtr, buffer);
            } finally {
                _free(buffer);
            }
        }

        let request;
        try {
            request = indexedDB.open(Config.DB_NAME, 1);
        } catch (exception) {
            fail(exception);
            return;
        }
        request.onupgradeneeded = function (event) {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(Config.STORE_NAME)) {
                db.createObjectStore(Config.STORE_NAME);
            }
        };

        request.onsuccess = function (event) {
            const db = event.target.result;
            if (completed) {
                closeDatabase(db);
                return;
            }

            try {
                const transaction = db.transaction(Config.STORE_NAME, "readwrite");
                const store = transaction.objectStore(Config.STORE_NAME);
                store.put(data, key);

                transaction.oncomplete = function () {
                    if (completed) {
                        return;
                    }

                    completed = true;
                    closeDatabase(db);
                    {{{ makeDynCall('vi', 'success') }}}(statePtr);
                };
                transaction.onerror = function (transactionEvent) {
                    fail(transactionEvent, db);
                };
                transaction.onabort = function (transactionEvent) {
                    fail(transactionEvent, db);
                };
            } catch (exception) {
                fail(exception, db);
            }
        };
        request.onerror = function (event) {
            fail(event);
        };
        request.onblocked = function () {
            fail(new Error("IndexedDB write was blocked."));
        };
    },

    DeleteFromIndexedDB: function (statePtr, keyStr, success, error) {
        const key = UTF8ToString(keyStr);
        let completed = false;

        function closeDatabase(db) {
            if (db) {
                db.close();
            }
        }

        function fail(reason, db) {
            if (completed) {
                closeDatabase(db);
                return;
            }

            completed = true;
            closeDatabase(db);
            const cause = reason && reason.target ? reason.target.error : reason;
            const message = cause && cause.message ? cause.message : "IndexedDB deletion failed.";
            const buffer = stringToNewUTF8(message);
            try {
                {{{ makeDynCall('vii', 'error') }}}(statePtr, buffer);
            } finally {
                _free(buffer);
            }
        }

        let request;
        try {
            request = indexedDB.open(Config.DB_NAME, 1);
        } catch (exception) {
            fail(exception);
            return;
        }
        request.onupgradeneeded = function (event) {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(Config.STORE_NAME)) {
                db.createObjectStore(Config.STORE_NAME);
            }
        };

        request.onsuccess = function (event) {
            const db = event.target.result;
            if (completed) {
                closeDatabase(db);
                return;
            }

            try {
                const transaction = db.transaction(Config.STORE_NAME, "readwrite");
                const store = transaction.objectStore(Config.STORE_NAME);
                store.delete(key);

                transaction.oncomplete = function () {
                    if (completed) {
                        return;
                    }

                    completed = true;
                    closeDatabase(db);
                    {{{ makeDynCall('vi', 'success') }}}(statePtr);
                };
                transaction.onerror = function (transactionEvent) {
                    fail(transactionEvent, db);
                };
                transaction.onabort = function (transactionEvent) {
                    fail(transactionEvent, db);
                };
            } catch (exception) {
                fail(exception, db);
            }
        };
        request.onerror = function (event) {
            fail(event);
        };
        request.onblocked = function () {
            fail(new Error("IndexedDB deletion was blocked."));
        };
    },

    LoadFromIndexedDB: function (statePtr, keyStr, success, error) {
        const key = UTF8ToString(keyStr);
        let completed = false;

        function closeDatabase(db) {
            if (db) {
                db.close();
            }
        }

        function fail(reason, db) {
            if (completed) {
                closeDatabase(db);
                return;
            }

            completed = true;
            closeDatabase(db);
            const cause = reason && reason.target ? reason.target.error : reason;
            const message = cause && cause.message ? cause.message : "IndexedDB read failed.";
            const buffer = stringToNewUTF8(message);
            try {
                {{{ makeDynCall('vii', 'error') }}}(statePtr, buffer);
            } finally {
                _free(buffer);
            }
        }

        let request;
        try {
            request = indexedDB.open(Config.DB_NAME, 1);
        } catch (exception) {
            fail(exception);
            return;
        }
        request.onupgradeneeded = function (event) {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(Config.STORE_NAME)) {
                db.createObjectStore(Config.STORE_NAME);
            }
        };

        request.onsuccess = function (event) {
            const db = event.target.result;
            if (completed) {
                closeDatabase(db);
                return;
            }

            try {
                const transaction = db.transaction(Config.STORE_NAME, "readonly");
                const store = transaction.objectStore(Config.STORE_NAME);
                const getRequest = store.get(key);

                getRequest.onsuccess = function () {
                    const result = getRequest.result;
                    if (result === undefined) {
                        fail(new Error("Key not found"), db);
                        return;
                    }

                    if (completed) {
                        closeDatabase(db);
                        return;
                    }

                    completed = true;
                    closeDatabase(db);
                    const bytes = result instanceof Uint8Array ? result : new Uint8Array(result);
                    const data = _malloc(bytes.length);
                    HEAPU8.set(bytes, data);
                    try {
                        {{{ makeDynCall('viii', 'success') }}}(statePtr, data, bytes.length);
                    } finally {
                        _free(data);
                    }
                };
                getRequest.onerror = function (getEvent) {
                    fail(getEvent, db);
                };
                transaction.onerror = function (transactionEvent) {
                    fail(transactionEvent, db);
                };
                transaction.onabort = function (transactionEvent) {
                    fail(transactionEvent, db);
                };
            } catch (exception) {
                fail(exception, db);
            }
        };
        request.onerror = function (event) {
            fail(event);
        };
        request.onblocked = function () {
            fail(new Error("IndexedDB read was blocked."));
        };
    },
};

autoAddDeps(plugin, '$Config');
mergeInto(LibraryManager.library, plugin);
