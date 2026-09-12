(() => {
    const dataNode = document.getElementById('flow-analysis-chart-data');
    if (!dataNode) return;

    const trend = JSON.parse(dataNode.textContent).trend ?? [];
    const chart = document.querySelector('.flow-chart');
    if (!chart || !trend.length) return;

    const svg = chart.querySelector('svg');
    const axis = chart.querySelector('[data-flow-axis]');
    const tooltip = chart.querySelector('[data-flow-tooltip]');
    if (!svg || !axis || !tooltip) return;

    const definitions = [
        { key: 'income', label: 'Rendimentos' },
        { key: 'expenses', label: 'Despesas' },
        { key: 'balance', label: 'Saldo' }
    ];
    const formatter = new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' });
    const left = 42;
    const right = 870;
    const top = 30;
    const bottom = 250;
    const values = trend.flatMap(item => definitions.map(definition => Number(item[definition.key]) || 0));
    let minimum = Math.min(...values, 0);
    let maximum = Math.max(...values, 0);
    if (minimum === maximum) maximum = minimum + 1;
    const padding = (maximum - minimum) * .08;
    minimum -= padding;
    maximum += padding;
    const x = index => trend.length === 1 ? (left + right) / 2 : left + index * (right - left) / (trend.length - 1);
    const y = value => bottom - (Number(value) - minimum) / (maximum - minimum) * (bottom - top);

    definitions.forEach(definition => {
        const line = chart.querySelector(`[data-series="${definition.key}"]`);
        if (!line) return;
        line.setAttribute('points', trend.map((item, index) =>
            `${x(index).toFixed(2)},${y(item[definition.key]).toFixed(2)}`).join(' '));
    });

    axis.replaceChildren(...trend.map(item => {
        const label = document.createElement('span');
        label.textContent = item.label;
        return label;
    }));

    svg.addEventListener('pointermove', event => {
        const bounds = svg.getBoundingClientRect();
        const pointerX = (event.clientX - bounds.left) / bounds.width * 900;
        const index = trend.reduce((best, _, candidate) =>
            Math.abs(x(candidate) - pointerX) < Math.abs(x(best) - pointerX) ? candidate : best, 0);
        const item = trend[index];
        const heading = document.createElement('strong');
        heading.textContent = item.label;
        const rows = definitions.map(definition => {
            const row = document.createElement('span');
            const label = document.createElement('b');
            label.textContent = definition.label;
            const value = document.createElement('em');
            value.textContent = formatter.format(Number(item[definition.key]) || 0);
            row.append(label, value);
            return row;
        });
        tooltip.replaceChildren(heading, ...rows);
        tooltip.hidden = false;
        const chartBounds = chart.getBoundingClientRect();
        tooltip.style.left = `${Math.max(0, event.clientX - chartBounds.left)}px`;
        tooltip.style.top = `${Math.max(20, event.clientY - chartBounds.top)}px`;
    });

    svg.addEventListener('pointerleave', () => { tooltip.hidden = true; });
})();
