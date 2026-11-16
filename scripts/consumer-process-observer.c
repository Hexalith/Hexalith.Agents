#define _GNU_SOURCE
#include <sys/ptrace.h>
#include <sys/types.h>
#include <sys/wait.h>
#include <signal.h>
#include <errno.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>

/* Kernel-owned process observation is independent of the traced program's environment and loader. */
static pid_t process_id(pid_t tid)
{
    char path[64], line[256];
    snprintf(path, sizeof(path), "/proc/%ld/status", (long)tid);
    FILE *status = fopen(path, "r");
    if (!status) return -1;
    long id = -1;
    while (fgets(line, sizeof(line), status)) if (sscanf(line, "Tgid: %ld", &id) == 1) break;
    fclose(status);
    return (pid_t)id;
}

int main(int argc, char **argv)
{
    if (argc < 5) { fprintf(stderr, "process observation: expected log, native trace prefix and command\n"); return 125; }
    FILE *log = fopen(argv[1], "a");
    if (!log) { perror("process observation log"); return 125; }
    setvbuf(log, NULL, _IOLBF, 0);
    int barrier[2];
    if (pipe2(barrier, O_CLOEXEC) < 0) { perror("process observation barrier"); return 125; }
    unsigned long options = PTRACE_O_TRACEFORK | PTRACE_O_TRACEVFORK | PTRACE_O_TRACECLONE | PTRACE_O_TRACEEXEC | PTRACE_O_EXITKILL;
    pid_t root = fork();
    if (root < 0) { perror("process observation fork"); return 125; }
    if (root == 0)
    {
        fclose(log);
        close(barrier[1]);
        char ready;
        ssize_t received;
        do { received = read(barrier[0], &ready, 1); } while (received < 0 && errno == EINTR);
        if (received != 1) _exit(125);
        close(barrier[0]);
        setenv("LD_DEBUG", "files", 1);
        setenv("LD_DEBUG_OUTPUT", argv[2], 1);
        execvp(argv[3], &argv[3]);
        perror("process observation exec");
        _exit(125);
    }
    close(barrier[0]);
    if (ptrace(PTRACE_SEIZE, root, 0, options) < 0 || ptrace(PTRACE_INTERRUPT, root, 0, 0) < 0)
    { perror("process observation requires ptrace seize/interrupt"); kill(root, SIGKILL); waitpid(root, NULL, 0); return 125; }
    int status, result = 125;
    pid_t initial;
    do { initial = waitpid(root, &status, 0); } while (initial < 0 && errno == EINTR);
    if (initial != root || !WIFSTOPPED(status))
    { fprintf(stderr, "process observation: ptrace unavailable or denied\n"); return 125; }
    if (write(barrier[1], "1", 1) != 1) { perror("process observation release"); return 125; }
    close(barrier[1]);
    fprintf(log, "created\t%ld\n", (long)root);
    ptrace(PTRACE_CONT, root, 0, 0);
    pid_t tid;
    for (;;)
    {
        tid = waitpid(-1, &status, __WALL);
        if (tid < 0 && errno == EINTR) continue;
        if (tid < 0) break;
        if (WIFEXITED(status) || WIFSIGNALED(status))
        {
            if (tid == root) result = WIFEXITED(status) ? WEXITSTATUS(status) : 128 + WTERMSIG(status);
            continue;
        }
        if (!WIFSTOPPED(status)) continue;
        unsigned event = (unsigned)status >> 16;
        if (event == PTRACE_EVENT_FORK || event == PTRACE_EVENT_VFORK || event == PTRACE_EVENT_CLONE)
        {
            unsigned long child = 0;
            if (ptrace(PTRACE_GETEVENTMSG, tid, 0, &child) < 0) { perror("process observation child"); return 125; }
            pid_t group = process_id((pid_t)child);
            if (group < 0) { fprintf(stderr, "process observation: child identity unavailable\n"); return 125; }
            if (group == (pid_t)child) fprintf(log, "created\t%ld\n", (long)child);
        }
        if (event == PTRACE_EVENT_EXEC) fprintf(log, "exec\t%ld\n", (long)tid);
        int signal = WSTOPSIG(status);
        if (event == PTRACE_EVENT_STOP && signal != SIGTRAP)
        {
            /* Preserve a real group stop until SIGCONT; real-parent WUNTRACED notifications remain intact. */
            if (ptrace(PTRACE_LISTEN, tid, 0, 0) < 0 && errno != ESRCH) { perror("process observation listen"); return 125; }
            continue;
        }
        if (event != 0) signal = 0; /* Seized newborn/interrupt stops are PTRACE_EVENT_STOP, not signal delivery. */
        if (ptrace(PTRACE_CONT, tid, 0, signal) < 0 && errno != ESRCH) { perror("process observation resume"); return 125; }
    }
    if (errno != ECHILD) { perror("process observation wait"); return 125; }
    fprintf(log, "complete\n");
    fclose(log);
    return result;
}
