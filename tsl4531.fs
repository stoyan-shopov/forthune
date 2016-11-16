\ read out the TSL4531 sensor
\ needs i2c

: tsl-init ( -- )
  +i2c
  $29 i2c-addr  $03 >i2c  0 i2c-xfer drop ;

: tsl-data ( -- v )
  $29 i2c-addr  $84 >i2c  2 i2c-xfer drop  i2c> i2c> 8 lshift or ;

\ tsl-init
\ tsl-data .
