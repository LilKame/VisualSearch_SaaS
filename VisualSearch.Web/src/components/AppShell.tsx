export function AppShell({ children } : { children: React.ReactNode})
{
    return(
        <div className="relative mx-auto flex min-h-dhv w-full max-w-app flex-col overflow-hidden">
            {children}
        </div>
    );
}
