(() => {
  const button = document.getElementById('savings-certificate-ai-paste');
  const form = document.getElementById('savings-certificate-form');
  const status = document.getElementById('savings-certificate-ai-status');
  if (!button || !form || !status) return;
  const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
  const fields = { investmentDate: '#InvestmentDate', seriesNumber: '#SeriesNumber', description: '#Description', investmentValue: '#InvestmentValue', currentValue: '#CurrentValue', nextCapitalization: '#NextCapitalization' };
  const show = (message, state) => { status.textContent = message; status.dataset.state = state; };
  const apply = suggestion => { let count = 0; Object.entries(fields).forEach(([key, selector]) => { if (suggestion[key] === null || suggestion[key] === undefined || suggestion[key] === '') return; const field = form.querySelector(selector); if (!field) return; field.value = suggestion[key]; field.dispatchEvent(new Event('input', { bubbles: true })); field.dispatchEvent(new Event('change', { bubbles: true })); count++; }); return count; };
  button.addEventListener('click', async () => {
    button.disabled = true; show('A ler e analisar a área de transferência…', 'loading');
    try {
      if (!navigator.clipboard?.readText) throw new Error('O browser não permite ler a área de transferência neste contexto.');
      const text = (await navigator.clipboard.readText()).trim(); if (!text) throw new Error('A área de transferência não contém texto.');
      const response = await fetch('/SavingsCertificates/SuggestFromClipboard', { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify({ text }) });
      const result = await response.json(); if (!response.ok) throw new Error(result.error || 'Não foi possível analisar os dados copiados.');
      const count = apply(result); const confidence = result.confidence === 'high' ? 'confiança elevada' : 'confiança reduzida';
      show(`${result.message} ${count} campo(s) preenchido(s), com ${confidence}. Reveja todos os valores antes de guardar.`, result.confidence);
      form.scrollIntoView({ behavior: 'smooth', block: 'start' });
    } catch (error) { show(error instanceof Error ? error.message : 'Não foi possível analisar os dados copiados.', 'error'); }
    finally { button.disabled = false; }
  });
})();
