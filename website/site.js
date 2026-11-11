const copyButton = document.querySelector("[data-copy]");
const copyStatus = document.querySelector("#copy-status");

copyButton?.addEventListener("click", async () => {
  const code = document.getElementById(copyButton.dataset.copy);
  const commands = Array.from(code.querySelectorAll("span"), line => line.textContent.trim()).join("\n");

  try {
    await navigator.clipboard.writeText(commands);
    copyButton.textContent = "Copied";
    copyStatus.textContent = "Commands copied to clipboard.";
  } catch {
    const range = document.createRange();
    range.selectNodeContents(code);
    const selection = window.getSelection();
    selection.removeAllRanges();
    selection.addRange(range);
    copyButton.textContent = "Commands selected";
    copyStatus.textContent = "Commands selected. Use your copy shortcut.";
  }

  window.setTimeout(() => {
    copyButton.textContent = "Copy commands";
    copyStatus.textContent = "";
  }, 3000);
});
