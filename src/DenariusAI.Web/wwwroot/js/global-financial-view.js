(() => {
    const dataNode = document.getElementById('global-financial-chart-data');
    if (!dataNode) return;

    const source = JSON.parse(dataNode.textContent);
    const trend = source.trend ?? [];
    if (!trend.length) return;

    const formatter = new Intl.NumberFormat('pt-PT', {
        style: 'currency',
        currency: 'EUR',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
    const chartDefinitions = {
        networth: [{ key: 'netWorth', label: 'Património' }],
        flows: [
            { key: 'income', label: 'Rendimentos' },
            { key: 'expenses', label: 'Despesas' },
            { key: 'savings', label: 'Poupança' }
        ]
    };
    const left = 38;
    const right = 870;
    const top = 30;
    const bottom = 230;

    document.querySelectorAll('[data-global-chart]').forEach(chart => {
        const series = chartDefinitions[chart.dataset.globalChart];
        if (!series) return;

        const svg = chart.querySelector('svg');
        const axis = chart.querySelector('[data-chart-axis]');
        const tooltip = chart.querySelector('[data-chart-tooltip]');
        if (!svg || !axis || !tooltip) return;

        const values = trend.flatMap(item => series.map(definition => Number(item[definition.key]) || 0));
        let minimum = Math.min(...values, 0);
        let maximum = Math.max(...values, 0);
        if (minimum === maximum) maximum = minimum + 1;
        const padding = (maximum - minimum) * 0.08;
        minimum -= padding;
        maximum += padding;

        const x = index => trend.length === 1
            ? (left + right) / 2
            : left + index * (right - left) / (trend.length - 1);
        const y = value => bottom - (Number(value) - minimum) / (maximum - minimum) * (bottom - top);

        series.forEach(definition => {
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
            const rows = series.map(definition => {
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
            const parentBounds = chart.getBoundingClientRect();
            tooltip.style.left = `${Math.max(0, event.clientX - parentBounds.left)}px`;
            tooltip.style.top = `${Math.max(20, event.clientY - parentBounds.top)}px`;
        });
        svg.addEventListener('pointerleave', () => { tooltip.hidden = true; });
    });
})();
