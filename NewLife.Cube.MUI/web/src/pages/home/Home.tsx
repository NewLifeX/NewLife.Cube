import { Box, Grid, Card, CardContent, Typography } from '@mui/material';

const statCards = [
  { title: '在线用户', value: '-' },
  { title: '今日访问', value: '-' },
  { title: '系统版本', value: '-' },
  { title: '运行时间', value: '-' },
];

export default function Home() {
  return (
    <Box>
      <Typography variant="h5" gutterBottom>首页</Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom>欢迎使用魔方管理平台</Typography>
      <Grid container spacing={2} sx={{ mt: 1 }}>
        {statCards.map((c) => (
          <Grid key={c.title} xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography color="text.secondary" gutterBottom>{c.title}</Typography>
                <Typography variant="h4">{c.value}</Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}
