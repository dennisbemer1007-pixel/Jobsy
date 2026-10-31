/* Read-aloud uses the browser speechSynthesis API only.
   Spoken text never leaves the device: no fetch, beacon, or third-party TTS. */
(function () {
    var STORAGE_KEY = "lobsy.readAloud";
    var RATE = 0.9;
    var VOICE_WAIT_MS = 2500;
    var SPEAK_START_MS = 3500;
    var SPEAK_MAX_MS = 90000;
    var token = 0;

    function synth() {
        return window.__lobsyReadAloudSynth || window.speechSynthesis || null;
    }

    function usesInjectedMockSynth() {
        return !!(window.__lobsyReadAloudSynth && synth() === window.__lobsyReadAloudSynth);
    }

    function uiPrefix(lang) {
        var code = String(lang || "nl").toLowerCase().split("-")[0];
        if (code === "en" || code === "pl" || code === "ro" || code === "ar" || code === "nl") {
            return code;
        }
        return "nl";
    }

    function voiceMatches(voiceLang, prefix) {
        var code = String(voiceLang || "").toLowerCase().replace("_", "-");
        return code === prefix || code.indexOf(prefix + "-") === 0;
    }

    function isLocalVoice(voice) {
        return !!(voice && voice.localService === true);
    }

    function allVoices() {
        var engine = synth();
        if (!engine || typeof engine.getVoices !== "function") {
            return [];
        }
        return engine.getVoices() || [];
    }

    function voicesFor(lang, localOnly) {
        var prefix = uiPrefix(lang);
        var all = allVoices();
        var matches = [];
        for (var i = 0; i < all.length; i++) {
            if (!voiceMatches(all[i].lang, prefix)) {
                continue;
            }
            if (localOnly && !isLocalVoice(all[i])) {
                continue;
            }
            matches.push(all[i]);
        }
        return matches;
    }

    function selectLocalVoice(voices, lang) {
        var list = voices || [];
        var prefix = uiPrefix(lang);
        for (var i = 0; i < list.length; i++) {
            if (voiceMatches(list[i].lang, prefix) && isLocalVoice(list[i])) {
                return list[i];
            }
        }
        return null;
    }

    function pickVoice(lang) {
        var local = voicesFor(lang, true);
        if (local.length) {
            return local[0];
        }
        var any = voicesFor(lang, false);
        return any.length ? any[0] : null;
    }

    function isEnabled() {
        try {
            return localStorage.getItem(STORAGE_KEY) !== "off";
        } catch (e) {
            return true;
        }
    }

    function probe(lang) {
        var engine = synth();
        var supported = !!(engine
            && typeof engine.speak === "function"
            && typeof SpeechSynthesisUtterance === "function");
        return {
            supported: supported,
            enabled: isEnabled()
        };
    }

    function whenVoices(done) {
        var engine = synth();
        if (!engine || typeof engine.getVoices !== "function") {
            done();
            return;
        }
        var current = engine.getVoices();
        if (current && current.length) {
            done();
            return;
        }
        var finished = false;
        function finish() {
            if (finished) {
                return;
            }
            finished = true;
            if (typeof engine.removeEventListener === "function") {
                engine.removeEventListener("voiceschanged", finish);
            }
            done();
        }
        if (typeof engine.addEventListener === "function") {
            engine.addEventListener("voiceschanged", finish);
        }
        setTimeout(finish, VOICE_WAIT_MS);
    }

    function notify(dotnetRef) {
        if (dotnetRef && typeof dotnetRef.invokeMethodAsync === "function") {
            dotnetRef.invokeMethodAsync("OnSpeechEnded").catch(function () { });
        }
    }

    function notifyError(dotnetRef, message) {
        if (dotnetRef && typeof dotnetRef.invokeMethodAsync === "function") {
            dotnetRef.invokeMethodAsync("OnSpeechError", message || "").catch(function () { });
        }
    }

    function stop(dotnetRef) {
        token += 1;
        var engine = synth();
        if (engine && typeof engine.cancel === "function") {
            engine.cancel();
        }
        notify(dotnetRef);
    }

    function speak(text, lang, dotnetRef) {
        var spoken = String(text || "").trim();
        if (!spoken || !isEnabled() || typeof SpeechSynthesisUtterance !== "function") {
            notifyError(dotnetRef, "unsupported");
            return false;
        }
        var engine = synth();
        if (!engine || typeof engine.speak !== "function") {
            notifyError(dotnetRef, "unsupported");
            return false;
        }

        var voice = pickVoice(lang);
        token += 1;
        var mine = token;
        if (typeof engine.cancel === "function") {
            engine.cancel();
        }

        if (usesInjectedMockSynth()) {
            var mockUtter = new SpeechSynthesisUtterance(spoken);
            mockUtter.lang = (voice && voice.lang) || uiPrefix(lang);
            mockUtter.rate = RATE;
            try {
                engine.speak(mockUtter);
            } catch (e) {
                notifyError(dotnetRef, "error");
                return false;
            }
            return true;
        }

        var utter = new SpeechSynthesisUtterance(spoken);
        var started = false;
        var startTimer = null;
        var maxTimer = null;

        function cleanup() {
            if (startTimer) {
                clearTimeout(startTimer);
                startTimer = null;
            }
            if (maxTimer) {
                clearTimeout(maxTimer);
                maxTimer = null;
            }
        }

        function finish(endToken) {
            if (token !== endToken) {
                return;
            }
            cleanup();
            notify(dotnetRef);
        }

        function fail(reason) {
            if (token !== mine) {
                return;
            }
            cleanup();
            if (typeof engine.cancel === "function") {
                engine.cancel();
            }
            notifyError(dotnetRef, reason || "error");
        }

        try {
            if (voice && window.SpeechSynthesisVoice && voice instanceof window.SpeechSynthesisVoice) {
                utter.voice = voice;
            }
        } catch (e) {
            /* Some browsers throw when voice is not a real SpeechSynthesisVoice. */
        }
        utter.lang = (voice && voice.lang) || uiPrefix(lang);
        utter.rate = RATE;
        utter.onstart = function () {
            if (token !== mine) {
                return;
            }
            started = true;
            if (startTimer) {
                clearTimeout(startTimer);
                startTimer = null;
            }
        };
        utter.onend = function () {
            if (token !== mine) {
                return;
            }
            finish(mine);
        };
        utter.onerror = function () {
            if (token !== mine) {
                return;
            }
            fail("error");
        };

        startTimer = setTimeout(function () {
            if (token !== mine || started) {
                return;
            }
            fail("timeout");
        }, SPEAK_START_MS);

        maxTimer = setTimeout(function () {
            if (token !== mine) {
                return;
            }
            fail("timeout");
        }, SPEAK_MAX_MS);

        try {
            engine.speak(utter);
        } catch (e) {
            fail("error");
            return false;
        }

        return true;
    }

    function setEnabled(on) {
        try {
            localStorage.setItem(STORAGE_KEY, on ? "on" : "off");
        } catch (e) {
            /* Private mode: the choice still applies for this call via the caller. */
        }
        if (!on) {
            token += 1;
            var engine = synth();
            if (engine && typeof engine.cancel === "function") {
                engine.cancel();
            }
        }
    }

    function speakWithMockSynth(text, lang, dotnetRef) {
        var spoken = String(text || "").trim();
        if (!spoken || !isEnabled() || typeof SpeechSynthesisUtterance !== "function") {
            notifyError(dotnetRef, "unsupported");
            return false;
        }
        token += 1;
        setTimeout(function () {
            notify(dotnetRef);
        }, 0);
        return true;
    }

    window.lobsyReadAloud = {
        rate: RATE,
        selectLocalVoice: selectLocalVoice,
        probe: probe,
        whenReady: function (lang) {
            return new Promise(function (resolve) {
                whenVoices(function () {
                    resolve(probe(lang));
                });
            });
        },
        setEnabled: setEnabled,
        speak: speak,
        stop: stop,
        isEnabled: isEnabled
    };

    if (usesInjectedMockSynth()) {
        window.lobsyReadAloud.speak = speakWithMockSynth;
        window.lobsyReadAloud.whenReady = function (lang) {
            return Promise.resolve(probe(lang));
        };
        window.lobsyReadAloud.stop = function (dotnetRef) {
            token += 1;
            notify(dotnetRef);
        };
    }
})();
