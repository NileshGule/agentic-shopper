import React from 'react';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  ChartOptions
} from 'chart.js';
import { Line, Bar, Pie } from 'react-chartjs-2';

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend
);

export interface LineChartProps {
  labels: string[];
  data: number[];
  label: string;
  color?: string;
  fillColor?: string;
}

export interface BarChartProps {
  labels: string[];
  data: number[];
  label: string;
  color?: string;
}

export interface PieChartProps {
  labels: string[];
  data: number[];
  colors?: string[];
}

/**
 * Reusable Line Chart component for trend visualization (T147)
 */
export const LineChart: React.FC<LineChartProps> = ({
  labels,
  data,
  label,
  color = 'rgb(75, 192, 192)',
  fillColor = 'rgba(75, 192, 192, 0.2)'
}) => {
  const chartData = {
    labels,
    datasets: [
      {
        label,
        data,
        borderColor: color,
        backgroundColor: fillColor,
        tension: 0.4, // Smooth curves
        fill: true
      }
    ]
  };

  const options: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'top' as const
      },
      title: {
        display: false
      },
      tooltip: {
        callbacks: {
          label: (context) => {
            return `${context.dataset.label}: $${context.parsed.y?.toFixed(2) ?? '0.00'}`;
          }
        }
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          callback: (value) => `$${value}`
        }
      }
    }
  };

  return (
    <div style={{ height: '300px', width: '100%' }}>
      <Line data={chartData} options={options} />
    </div>
  );
};

/**
 * Reusable Bar Chart component for store/category comparisons (T149)
 */
export const BarChart: React.FC<BarChartProps> = ({
  labels,
  data,
  label,
  color = 'rgb(54, 162, 235)'
}) => {
  const chartData = {
    labels,
    datasets: [
      {
        label,
        data,
        backgroundColor: color,
        borderColor: color.replace('rgb', 'rgba').replace(')', ', 1)'),
        borderWidth: 1
      }
    ]
  };

  const options: ChartOptions<'bar'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'top' as const
      },
      title: {
        display: false
      },
      tooltip: {
        callbacks: {
          label: (context) => {
            return `${context.dataset.label}: $${context.parsed.y?.toFixed(2) ?? '0.00'}`;
          }
        }
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          callback: (value) => `$${value}`
        }
      }
    }
  };

  return (
    <div style={{ height: '300px', width: '100%' }}>
      <Bar data={chartData} options={options} />
    </div>
  );
};

/**
 * Reusable Pie Chart component for category breakdown (T148)
 */
export const PieChart: React.FC<PieChartProps> = ({ labels, data, colors }) => {
  const defaultColors = [
    'rgb(255, 99, 132)',
    'rgb(54, 162, 235)',
    'rgb(255, 206, 86)',
    'rgb(75, 192, 192)',
    'rgb(153, 102, 255)',
    'rgb(255, 159, 64)',
    'rgb(201, 203, 207)',
    'rgb(255, 99, 71)',
    'rgb(144, 238, 144)',
    'rgb(255, 215, 0)',
    'rgb(173, 216, 230)'
  ];

  const chartData = {
    labels,
    datasets: [
      {
        data,
        backgroundColor: colors || defaultColors.slice(0, labels.length),
        borderColor: colors?.map((c) =>
          c.replace('rgb', 'rgba').replace(')', ', 1)')
        ) || defaultColors.slice(0, labels.length).map((c) =>
          c.replace('rgb', 'rgba').replace(')', ', 1)')
        ),
        borderWidth: 1
      }
    ]
  };

  const options: ChartOptions<'pie'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'right' as const,
        labels: {
          generateLabels: (chart) => {
            const data = chart.data;
            if (data.labels && data.datasets.length) {
              return data.labels.map((label, i) => {
                const value = data.datasets[0].data[i] as number;
                const total = (data.datasets[0].data as number[]).reduce(
                  (sum, val) => sum + val,
                  0
                );
                const percentage = ((value / total) * 100).toFixed(1);
                return {
                  text: `${label}: $${value.toFixed(2)} (${percentage}%)`,
                  fillStyle: (data.datasets[0].backgroundColor as string[])[i],
                  hidden: false,
                  index: i
                };
              });
            }
            return [];
          }
        }
      },
      tooltip: {
        callbacks: {
          label: (context) => {
            const value = context.parsed;
            const total = (context.dataset.data as number[]).reduce(
              (sum, val) => sum + val,
              0
            );
            const percentage = ((value / total) * 100).toFixed(1);
            return `${context.label}: $${value.toFixed(2)} (${percentage}%)`;
          }
        }
      }
    }
  };

  return (
    <div style={{ height: '400px', width: '100%' }}>
      <Pie data={chartData} options={options} />
    </div>
  );
};
