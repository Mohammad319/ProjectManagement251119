// Helper to download an ATACOST package (.atacost) that the client received as base64 from the server.
(function () {
    function base64ToBytes(base64) {
        var binary = atob(base64);
        var len = binary.length;
        var bytes = new Uint8Array(len);
        for (var i = 0; i < len; i++) bytes[i] = binary.charCodeAt(i);
        return bytes;
    }

    window.atacostTransfer = {
        // fileName: e.g. "ATACOST_Projekt_X_2026-06-19.atacost", base64: the package content
        download: function (fileName, base64) {
            try {
                var blob = new Blob([base64ToBytes(base64)], { type: 'application/octet-stream' });
                var url = URL.createObjectURL(blob);
                var link = document.createElement('a');
                link.href = url;
                link.download = fileName;
                document.body.appendChild(link);
                link.click();
                link.remove();
                setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
                return true;
            } catch (e) {
                console.error('atacostTransfer.download failed', e);
                return false;
            }
        }
    };
})();
