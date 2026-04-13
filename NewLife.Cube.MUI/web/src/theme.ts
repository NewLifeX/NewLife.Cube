import { createTheme } from '@mui/material/styles';

export function createAppTheme(darkMode: boolean) {
  return createTheme({
    palette: {
      mode: darkMode ? 'dark' : 'light',
      primary: { main: '#1976d2' },
      secondary: { main: '#9c27b0' },
    },
    typography: {
      fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
    },
    components: {
      MuiButton: {
        defaultProps: { size: 'small' },
      },
      MuiTextField: {
        defaultProps: { size: 'small', variant: 'outlined' },
      },
    },
  });
}
