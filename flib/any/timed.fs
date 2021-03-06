\ timed.fs
\ Multiple callback timers using one single background task
\ Needs multi.fs

\ Temporary - for easier debugging
\ <<<board>>>
\ compiletoflash
\ include ../flib/mecrisp/multi.fs
\ End Temporary

\ --------------------------------------------------
\  Configuration
\ --------------------------------------------------

\ maximum number of timers (using 4 cells each)
[ifndef] MAX-TIMED  8 constant MAX-TIMED [then]

\ --------------------------------------------------
\  Internal Helpers
\ --------------------------------------------------

\ 4 cells per timer ( interval, last-run, callback, repeat )
MAX-TIMED 4 * cells buffer: timed-data

\ Calculate internal adresses
: tmd-inte-addr ( slot# - addr ) timed-data swap 4 *     cells + ;
: tmd-last-addr ( slot# - addr ) timed-data swap 4 * 1+  cells + ;
: tmd-call-addr ( slot# - addr ) timed-data swap 4 * 2+  cells + ;
: tmd-repe-addr ( slot# - addr ) timed-data swap 4 * 3 + cells + ;

\ Used by call-once/call-periodic
: call-internal ( callback when/interval repeat slot# -- )
  >r     r@ tmd-repe-addr !
         r@ tmd-inte-addr !
         r@ tmd-call-addr !
  millis r> tmd-last-addr !
;

\ Execute timer callback and clear if no repetition is required
: timed-exec ( slot# -- )
  >r     r@ tmd-call-addr @ execute
  millis r@ tmd-last-addr !
  r@ tmd-repe-addr @ NOT IF
    \ clear callback if no repetition needed
    false r@ tmd-call-addr !
  THEN 
  r> drop 
;

\ Check if a timer needs to be executed (checks if enabled and enough time passed)
: needs-run? ( slot# -- flag )
  dup tmd-call-addr @ IF 
    millis over tmd-last-addr @ -  ( slot# time_since_last_run )
           over tmd-inte-addr @ >  ( slot# true_if_time_to_run )
  ELSE
    false
  THEN nip ;

\ Check and execute all the timers
: timed-run ( -- #exec )
  0 \ return number of executed tasks
  MAX-TIMED 0 DO
  i needs-run? IF
    i timed-exec 1+
  THEN
LOOP ;

\ Go to sleep if we're the only task running
: sleep-if-alone ( -- )
  eint? IF \ Only enter sleep mode if interrupts have been enabled
    dint up-alone? IF sleep THEN eint
  THEN ;

\ Task which handles the timers in background
task: timedtask
: timed& ( -- )
  timedtask activate
    begin
      timed-run NOT IF sleep-if-alone THEN
      pause
    again
;

\ --------------------------------------------------
\  External API
\ --------------------------------------------------

\ Clear timer data structure
: clear-timed ( -- ) timed-data MAX-TIMED 4 * cells 0 fill ;

\ Register a callback or cancel a timer
: call-after ( callback when     slot# -- ) false swap call-internal ; 
: call-every ( callback interval slot# -- ) true  swap call-internal ; 
: call-never ( slot# -- ) tmd-call-addr 0 swap !  ;

\ Show all timers
: timed. ( -- ) cr
  MAX-TIMED 0 do
    ." Timer #" i .
    ." Interval: " i tmd-inte-addr @ .
    ." Last-Run: " i tmd-last-addr @ .
    ." Callback: " i tmd-call-addr @ .
    ." Repeat: "   i tmd-repe-addr @ .
  cr loop ;

\ Initializes timed-data and starts multitasking
: timed-init ( -- )
  clear-timed
  timed& multitask
;

\ for testing only
\ : ping ( -- ) cr ." PING" cr ;

