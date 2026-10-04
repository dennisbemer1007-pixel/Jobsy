/* Read-aloud uses the browser speechSynthesis API only.
   Spoken text never leaves the device: no fetch, beacon, or third-party TTS. */
(function () {
    var STORAGE_KEY = "lobsy.readAloud";
    var RATE = 0.9;
    var token = 0;

    function synth() {
        return window.__lobsyReadAloudSynth || window.speechSynthesis || null;
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

    function voicesFor(lang) {
        var engine = synth();
        if (!engine || typeof engine.getVoices !== "function") {
            return [];
        }
        var prefix = uiPrefix(lang);
        var all = engine.getVoices() || [];
        var matches = [];
        for (var i = 0; i < all.length; i++) {
            if (voiceMatches(all[i].lang, prefix)) {
                matches.push(all[i]);
            }
        }
        return matches;
    }

    function isEnabled() {
        try {
            return localStorage.getItem(STORAGE_KEY) !== "off";
        } catch (e) {
            return true;
        }
    }

    function probe(lang) {
        return {
            supported: voicesFor(lang).length > 0,
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
        setTimeout(finish, 800);
    }

    function notify(dotnetRef) {
        if (dotnetRef && typeof dotnetRef.invokeMethodAsync === "function") {
            dotnetRef.invokeMethodAsync("OnSpeechEnded").catch(function () { });
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
        var voices = voicesFor(lang);
        if (!spoken || !isEnabled() || voices.length === 0 || typeof SpeechSynthesisUtterance !== "function") {
            return false;
        }
        var engine = synth();
        if (!engine || typeof engine.speak !== "function") {
            return false;
        }

        token += 1;
        var mine = token;
        if (typeof engine.cancel === "function") {
            engine.cancel();
        }

        var utter = new SpeechSynthesisUtterance(spoken);
        try {
            if (voices[0] && window.SpeechSynthesisVoice && voices[0] instanceof window.SpeechSynthesisVoice) {
                utter.voice = voices[0];
            }
        } catch (e) {
            /* Some browsers throw when voice is not a real SpeechSynthesisVoice. */
        }
        utter.lang = (voices[0] && voices[0].lang) || uiPrefix(lang);
        utter.rate = RATE;
        utter.onend = utter.onerror = function () {
            if (token !== mine) {
                return;
            }
            notify(dotnetRef);
        };
        engine.speak(utter);
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

    window.lobsyReadAloud = {
        rate: RATE,
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
})();
