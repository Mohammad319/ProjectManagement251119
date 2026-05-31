(function () {
    function escapeHtml(value) {
        return String(value ?? '')
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    function buildTable(title, columns, rows) {
        const header = columns.map(column => '<th>' + escapeHtml(column) + '</th>').join('');
        const body = rows.map(row =>
            '<tr>' + row.map(value => '<td>' + escapeHtml(value) + '</td>').join('') + '</tr>'
        ).join('');

        return '<h1>' + escapeHtml(title) + '</h1>' +
            '<table><thead><tr>' + header + '</tr></thead><tbody>' + body + '</tbody></table>';
    }

    function documentHtml(title, columns, rows) {
        return '<!DOCTYPE html><html><head><meta charset="utf-8"><title>' + escapeHtml(title) + '</title>' +
            '<style>' +
            'body{margin:8mm;font-family:system-ui,-apple-system,sans-serif;color:#0f172a;font-size:11px}' +
            'h1{margin:0 0 12px;font-size:18px}' +
            'table{border-collapse:collapse;width:100%}' +
            'thead{display:table-header-group}' +
            'th,td{border:1px solid #cbd5e1;padding:5px 7px;text-align:left;vertical-align:top}' +
            'th{background:#f1f5f9;font-weight:700}' +
            'tr{page-break-inside:avoid}' +
            '</style></head><body>' + buildTable(title, columns, rows) + '</body></html>';
    }

    function download(blob, fileName) {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    }

    window.pmReportExport = {
        excel: function (fileName, title, columns, rows) {
            const html = documentHtml(title, columns || [], rows || []);
            download(new Blob(['\ufeff', html], { type: 'application/vnd.ms-excel;charset=utf-8' }), fileName + '.xls');
        },
        print: function (title, columns, rows, pdfMode) {
            const win = window.open('', '_blank', 'width=1400,height=900');
            if (!win) return;
            win.document.write(documentHtml(title + (pdfMode ? ' - PDF' : ''), columns || [], rows || []));
            win.document.close();
            win.focus();
            setTimeout(function () { win.print(); win.close(); }, 350);
        }
    };
})();
