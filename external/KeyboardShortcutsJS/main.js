document.addEventListener("keydown", function (event) {
  if (event.ctrlKey && event.key === "l") {
    event.preventDefault();
    window.chrome.webview.postMessage("ControlL");
  }
  if (event.ctrlKey && event.key === "t") {
    event.preventDefault();
    window.chrome.webview.postMessage("ControlT");
  }
});