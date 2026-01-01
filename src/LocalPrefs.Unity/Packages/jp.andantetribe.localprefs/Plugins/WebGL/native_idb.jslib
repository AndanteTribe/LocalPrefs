const plugin = {

    $Config: { DB_NAME : "/localprefs", STORE_NAME : "MAIN", DEBUG_STORE_NAME : "DEBUG" },

    SaveToIndexedDB: function (statePtr, keyStr, dataPtr, dataSize, success, error) {
        const key = UTF8ToString(keyStr);
        const data = new Uint8Array(dataSize);
        for (let i = 0; i < dataSize; i++) {
            data[i] = HEAPU8[dataPtr + i];
        }

        const request = indexedDB.open(Config.DB_NAME, 1);
        request.onupgradeneeded = function (event) {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(Config.STORE_NAME)) {
                db.createObjectStore(Config.STORE_NAME);
            }
        };

        request.onsuccess = function (event) {
            const db = event.target.result;
            const transaction = db.transaction(Config.STORE_NAME, "readwrite");
            const store = transaction.objectStore(Config.STORE_NAME);
            store.put(data, key);

            transaction.oncomplete = function () {
                {{{ makeDynCall('vi', 'success') }}} (statePtr);
                db.close();
            };

            transaction.onerror = function (event) {
                const buffer = stringToNewUTF8(event.target.error.message);
                {{{ makeDynCall('vii', 'error') }}} (statePtr, buffer);
                _free(buffer);
                db.close();
            };
        };

        request.onerror = function (event) {
            const buffer = stringToNewUTF8(event.target.error.message);
            {{{ makeDynCall('vii', 'error') }}} (statePtr, buffer);
            _free(buffer);
        };
    },

    DeleteFromIndexedDB: function(statePtr, keyStr, success, error) {
        const key = UTF8ToString(keyStr);

        const request = indexedDB.open(Config.DB_NAME, 1);
        request.onupgradeneeded = function (event) {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(Config.STORE_NAME)) {
                db.createObjectStore(Config.STORE_NAME);
            }
        };

        request.onsuccess = function (event) {
            const db = event.target.result;
            const transaction = db.transaction(Config.STORE_NAME, "readwrite");
            const store = transaction.objectStore(Config.STORE_NAME);
            store.delete(key);

            transaction.oncomplete = function () {
                {{{ makeDynCall('vi', 'success') }}} (statePtr);
                db.close();
            };

            transaction.onerror = function (event) {
                const buffer = stringToNewUTF8(event.target.error.message);
                {{{ makeDynCall('vii', 'error') }}} (statePtr, buffer);
                _free(buffer);
                db.close();
            };
        };

        request.onerror = function (event) {
            const buffer = stringToNewUTF8(event.target.error.message);
            {{{ makeDynCall('vii', 'error') }}} (statePtr, buffer);
            _free(buffer);
        };
    },

    LoadFromIndexedDB: function (statePtr, keyStr, success, error) {
        const key = UTF8ToString(keyStr);

        const request = indexedDB.open(Config.DB_NAME, 1);
        request.onupgradeneeded = function (event) {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(Config.STORE_NAME)) {
                db.createObjectStore(Config.STORE_NAME);
            }
        };

        request.onsuccess = function (event) {
            const db = event.target.result;
            const transaction = db.transaction(Config.STORE_NAME, "readonly");
            const store = transaction.objectStore(Config.STORE_NAME);
            const getRequest = store.get(key);

            getRequest.onsuccess = function () {
                const result = getRequest.result;
                if (result) {
                    const length = result.length;
                    const data = _malloc(length);
                    HEAPU8.set(result, data);
                    {{{ makeDynCall('viii', 'success') }}} (statePtr, data, length);
                    _free(data);
                } else {
                    const buffer = stringToNewUTF8("Key not found");
                    {{{ makeDynCall('vii', 'error') }}} (statePtr, buffer);
                    _free(buffer);
                }
                db.close();
            };

            getRequest.onerror = function (event) {
                const buffer = stringToNewUTF8(event.target.error.message);
                {{{ makeDynCall('vii', 'error') }}} (statePtr, buffer);
                _free(buffer);
                db.close();
            };
        };

        request.onerror = function (event) {
            const buffer = stringToNewUTF8(event.target.error.message);
            {{{ makeDynCall('vii', 'error') }}} (statePtr, buffer);
            _free(buffer);
        };
    },
};

autoAddDeps(plugin, '$Config');
mergeInto(LibraryManager.library, plugin);