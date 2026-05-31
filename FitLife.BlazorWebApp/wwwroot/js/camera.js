window.fitlifeCamera = (() => {
    let _stream = null;

    async function start(videoId) {
        try {
            _stream = await navigator.mediaDevices.getUserMedia({
                video: { width: { ideal: 640 }, height: { ideal: 480 }, facingMode: 'user' },
                audio: false
            });
            const v = document.getElementById(videoId);
            if (!v) return false;
            v.srcObject = _stream;
            await v.play();
            return true;
        } catch {
            return false;
        }
    }

    function stop() {
        if (_stream) { _stream.getTracks().forEach(t => t.stop()); _stream = null; }
    }

    function capture(videoId, canvasId) {
        const v = document.getElementById(videoId);
        const c = document.getElementById(canvasId);
        if (!v || !c) return null;
        c.width  = v.videoWidth  || 640;
        c.height = v.videoHeight || 480;
        c.getContext('2d').drawImage(v, 0, 0, c.width, c.height);
        return c.toDataURL('image/jpeg', 0.92);   // returns "data:image/jpeg;base64,..."
    }

    return { start, stop, capture };
})();