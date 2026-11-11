import { QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Link, Route, Routes } from 'react-router';
import { createQueryClient } from './api/queryClient';
import { ErrorBoundary } from './components/Feedback';
import { Layout } from './components/Layout';
import { ClassListPage } from './pages/ClassListPage';
import { CreateClassPage } from './pages/CreateClassPage';
import { HoldPage } from './pages/HoldPage';
import { MyHoldsPage } from './pages/MyHoldsPage';
import { StudentProvider } from './student/StudentContext';

const queryClient = createQueryClient();

function NotFoundPage() {
  return (
    <section>
      <h1>Page not found</h1>
      <Link to="/">Back to the class list</Link>
    </section>
  );
}

export default function App() {
  return (
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <StudentProvider>
          <BrowserRouter>
            <Routes>
              <Route element={<Layout />}>
                <Route index element={<ClassListPage />} />
                <Route path="admin/classes/new" element={<CreateClassPage />} />
                <Route path="holds/:holdId" element={<HoldPage />} />
                <Route path="me" element={<MyHoldsPage />} />
                <Route path="*" element={<NotFoundPage />} />
              </Route>
            </Routes>
          </BrowserRouter>
        </StudentProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  );
}
