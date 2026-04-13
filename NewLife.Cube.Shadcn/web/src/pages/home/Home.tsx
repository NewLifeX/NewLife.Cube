export default function Home() {
  const stats = [
    { title: '在线用户', value: '-' },
    { title: '今日访问', value: '-' },
    { title: '系统版本', value: '-' },
    { title: '运行时间', value: '-' },
  ];

  return (
    <div>
      <h1 className="text-2xl font-bold">首页</h1>
      <p className="mt-1 text-sm text-muted-foreground">欢迎使用魔方管理平台</p>
      <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map((s) => (
          <div key={s.title} className="rounded-lg border bg-card p-6">
            <p className="text-sm text-muted-foreground">{s.title}</p>
            <p className="mt-2 text-3xl font-bold">{s.value}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
