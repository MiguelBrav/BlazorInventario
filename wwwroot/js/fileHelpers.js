window.downloadFileFromBase64 = (filename, base64) => {
    try {
        const link = document.createElement('a');
        link.href = 'data:text/csv;charset=utf-8;base64,' + base64;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        link.remove();
    } catch (e) {
        console.error('downloadFileFromBase64 error', e);
    }
};
