(() => {
    const dataNode = document.getElementById('dashboard-chart-data');
    if (!dataNode) return;

    const data = JSON.parse(dataNode.textContent);
    const euroFormatter = new Intl.NumberFormat('pt-PT', {
        style: 'currency',
        currency: 'EUR',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
    const svgNamespace = 'http://www.w3.org/2000/svg';
    const left = 28;
    const right = 872;
    const top = 30;
    const bottom = 236;

    const chartDefinitions = {
        budget: {
            type: 'bar',
            items: data.budget ?? [],
            axisLabel: item => item.label,
            tooltipLabel: item => item.label,
            series: [
                { key: 'budgeted', label: 'Orçamentado' },
                { key: 'actual', label: 'Realizado' }
            ]
        },
        annual: {
            type: 'line',
            items: data.annual ?? [],
            axisLabel: item => item.axisLabel,
            tooltipLabel: item => item.label,
            series: [
                { key: 'income', label: 'Rendimentos' },
                { key: 'expenses', label: 'Despesas' }
            ]
        }
    };

    document.querySelectorAll('[data-dashboard-chart]').forEach(chart => {
        const definition = chartDefinitions[chart.dataset.dashboardChart];
        if (!definition?.items.length) return;

        const stage = chart.querySelector('.dashboard-chart-stage');
        const svg = chart.querySelector('svg');
        const axis = chart.querySelector('[data-chart-axis]');
        if (!stage || !svg || !axis) return;

        const values = definition.items.flatMap(item => definition.series.map(series => Number(item[series.key]) || 0));
        const maximum = Math.max(...values, 1);
        const paddedMaximum = maximum * 1.08;
        const x = index => definition.type === 'bar'
            ? left + (index + 0.5) * (right - left) / definition.items.length
            : definition.items.length <= 1
                ? (left + right) / 2
                : left + index * (right - left) / (definition.items.length - 1);
        const y = value => bottom - Math.max(0, Number(value)) / paddedMaximum * (bottom - top);
        const barsByIndex = definition.items.map(() => []);

        if (definition.type === 'bar') {
            const groupWidth = (right - left) / definition.items.length;
            const barGap = Math.min(10, Math.max(4, groupWidth * 0.08));
            const barWidth = Math.min(38, Math.max(12, (groupWidth - barGap) * 0.32));
            const pairWidth = definition.series.length * barWidth + (definition.series.length - 1) * barGap;

            definition.series.forEach((series, seriesIndex) => {
                const group = chart.querySelector(`[data-chart-bars="${series.key}"]`);
                if (!group) return;

                const bars = definition.items.map((item, index) => {
                    const value = Math.max(0, Number(item[series.key]) || 0);
                    const rect = document.createElementNS(svgNamespace, 'rect');
                    const barX = x(index) - pairWidth / 2 + seriesIndex * (barWidth + barGap);
                    const barY = y(value);
                    rect.classList.add('dashboard-chart-bar', `${series.key}-bar`);
                    rect.setAttribute('x', barX.toFixed(2));
                    rect.setAttribute('y', barY.toFixed(2));
                    rect.setAttribute('width', barWidth.toFixed(2));
                    rect.setAttribute('height', Math.max(0, bottom - barY).toFixed(2));
                    rect.setAttribute('rx', '7');
                    rect.setAttribute('ry', '7');
                    barsByIndex[index].push(rect);
                    return rect;
                });
                group.replaceChildren(...bars);
            });
        } else {
            definition.series.forEach(series => {
                const points = definition.items
                    .map((item, index) => `${x(index).toFixed(2)},${y(item[series.key]).toFixed(2)}`)
                    .join(' ');
                const polyline = chart.querySelector(`[data-chart-series="${series.key}"]`);
                if (polyline) polyline.setAttribute('points', points);

                const area = chart.querySelector(`[data-chart-area="${series.key}"]`);
                if (area) {
                    area.setAttribute('points', `${x(0).toFixed(2)},${bottom} ${points} ${x(definition.items.length - 1).toFixed(2)},${bottom}`);
                }
            });
        }

        axis.style.setProperty('--chart-points', definition.items.length);
        axis.replaceChildren(...definition.items.map(item => {
            const label = document.createElement('span');
            label.textContent = definition.axisLabel(item);
            label.title = definition.tooltipLabel(item);
            return label;
        }));

        let hoverLine = null;
        let hoverPoints = [];
        if (definition.type === 'line') {
            hoverLine = document.createElementNS(svgNamespace, 'line');
            hoverLine.classList.add('dashboard-chart-hover-line');
            hoverLine.setAttribute('y1', top);
            hoverLine.setAttribute('y2', bottom);
            svg.append(hoverLine);

            hoverPoints = definition.series.map(series => {
                const point = document.createElementNS(svgNamespace, 'circle');
                point.classList.add('dashboard-chart-hover-point', `${series.key}-hover-point`);
                point.setAttribute('r', '5');
                svg.append(point);
                return { ...series, point };
            });
        }

        const tooltip = document.createElement('div');
        tooltip.className = 'dashboard-chart-tooltip';
        tooltip.setAttribute('role', 'status');
        stage.append(tooltip);

        let activeBarIndex = -1;
        const setActiveBars = index => {
            if (definition.type !== 'bar' || activeBarIndex === index) return;
            if (activeBarIndex >= 0) barsByIndex[activeBarIndex].forEach(bar => bar.classList.remove('active'));
            barsByIndex[index].forEach(bar => bar.classList.add('active'));
            activeBarIndex = index;
        };

        const renderTooltip = (item, index, event) => {
            const selectedX = x(index);
            if (hoverLine) {
                hoverLine.setAttribute('x1', selectedX);
                hoverLine.setAttribute('x2', selectedX);
                hoverLine.classList.add('visible');
            }

            hoverPoints.forEach(series => {
                series.point.setAttribute('cx', selectedX);
                series.point.setAttribute('cy', y(item[series.key]));
                series.point.classList.add('visible');
            });
            setActiveBars(index);

            const heading = document.createElement('strong');
            heading.textContent = definition.tooltipLabel(item);
            const valuesContainer = document.createElement('div');
            valuesContainer.className = 'dashboard-chart-tooltip-values';

            definition.series.forEach(series => {
                const row = document.createElement('span');
                row.className = `dashboard-chart-tooltip-row ${series.key}`;
                const label = document.createElement('b');
                label.textContent = series.label;
                const value = document.createElement('em');
                value.textContent = euroFormatter.format(Number(item[series.key]) || 0);
                row.append(label, value);
                valuesContainer.append(row);
            });

            tooltip.replaceChildren(heading, valuesContainer);
            tooltip.classList.add('visible');

            const stageBounds = stage.getBoundingClientRect();
            const tooltipX = event.clientX - stageBounds.left;
            const tooltipY = event.clientY - stageBounds.top;
            tooltip.style.left = `${tooltipX}px`;
            tooltip.style.top = `${tooltipY}px`;
            tooltip.classList.toggle('align-left', tooltipX > stageBounds.width * 0.7);
        };

        svg.addEventListener('pointermove', event => {
            const bounds = svg.getBoundingClientRect();
            const pointerX = (event.clientX - bounds.left) / bounds.width * 900;
            const nearestIndex = definition.items.reduce((bestIndex, _, index) =>
                Math.abs(x(index) - pointerX) < Math.abs(x(bestIndex) - pointerX) ? index : bestIndex, 0);
            renderTooltip(definition.items[nearestIndex], nearestIndex, event);
        });

        svg.addEventListener('pointerleave', () => {
            tooltip.classList.remove('visible');
            hoverLine?.classList.remove('visible');
            hoverPoints.forEach(series => series.point.classList.remove('visible'));
            if (activeBarIndex >= 0) barsByIndex[activeBarIndex].forEach(bar => bar.classList.remove('active'));
            activeBarIndex = -1;
        });
    });
})();
